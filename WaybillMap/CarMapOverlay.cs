using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using HarmonyLib;
using Model;
using Model.Ops;
using Track;
using UI.Map;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
{
    [DefaultExecutionOrder(1100)]
    internal sealed class CarMapOverlay : MonoBehaviour
    {
        private RawImage? image;
        private MapBuilder? builder;
        private Window? window;
        private GameObject? root;
        private VehicleLayer? wagons, engines;
        private readonly NativeMapIcons native = new NativeMapIcons();
        private readonly List<BaseLocomotive> locomotives = new List<BaseLocomotive>();
        private readonly List<Entry> cars = new List<Entry>();
        private Dictionary<Car, Car> tenders = new Dictionary<Car, Car>();
        private bool stopped, warned;
        private float nextRefresh;
        private static readonly Color Unassigned = new Color32(MarkerStyle.UnassignedChannel, MarkerStyle.UnassignedChannel, MarkerStyle.UnassignedChannel, 255);
        private static readonly System.Reflection.FieldInfo TrackWidth = AccessTools.Field(typeof(MapBuilder), "_segmentLineWidth");
        private sealed class Entry
        {
            internal Car Car = null!;
            internal Color Color;
            internal string Label = "", Number = "";
            internal int Index;
            internal double AverageLength;
        }

        internal void Initialize(RawImage mapImage, MapBuilder mapBuilder)
        {
            image = mapImage; builder = mapBuilder; window = GetComponent<Window>(); nextRefresh = 0;
            if (root != null) return;
            var rect = VehicleLayer.CreateRect("CzBuCHi.WaybillMap.Clip", image.rectTransform);
            root = rect.gameObject; root.AddComponent<RectMask2D>();
            wagons = new VehicleLayer(VehicleLayer.CreateRect("Wagons", rect));
            engines = new VehicleLayer(VehicleLayer.CreateRect("Locomotives", rect)); // Entire locomotive layer is last.
            root.SetActive(false);
        }

        private void Refresh(TrainController controller)
        {
            cars.Clear(); locomotives.Clear();
            var registered = controller.Cars.Where(c => c != null).OrderBy(c => c.id, StringComparer.Ordinal).ToArray();
            tenders = TenderPairing.Find(registered, c => c is SteamLocomotive steam && steam.hasTender,
                c => c.set == null ? null : c.CoupledTo(c.EndToLogical(Car.End.R)), c => c.CarType.IsTender);
            var merged = new HashSet<Car>(tenders.Values);
            var seen = new HashSet<Car>();
            foreach (var anchor in registered)
            {
                if (seen.Contains(anchor)) continue;
                var group = anchor.set == null ? new[] { anchor } : anchor.EnumerateCoupled().Where(c => c != null).ToArray();
                if (group.Length == 0) group = new[] { anchor };
                var average = group.Sum(c => Math.Max(1f, c.carLength)) / Math.Max(1, group.Count(c => !merged.Contains(c)));
                var index = 0;
                for (var i = 0; i < group.Length; i++)
                {
                    var car = group[i]; if (!seen.Add(car)) continue;
                    if (merged.Contains(car)) continue;
                    if (car is BaseLocomotive locomotive) locomotives.Add(locomotive);
                    try { cars.Add(new Entry { Car = car, Color = ColorFor(car), Index = index++, AverageLength = average,
                        Number = car.Ident.RoadNumber, Label = MarkerStyle.Label(car.Ident.ReportingMark, car.Ident.RoadNumber, car.CarType.IsTender) }); }
                    catch (Exception error) { Report(error); }
                }
            }
        }

        private static Color ColorFor(Car car)
        {
            var ops = OpsController.Shared;
            if (ops == null || !ops.TryGetDestinationInfo(car, out _, out var spotted, out _, out var destination))
                return Unassigned;
            // Destination info includes repair/depot overrides, including locomotives without a freight waybill.
            var tint = ops.AreaForCarPosition(destination)?.tagColor ?? Unassigned;
            return new Color(MarkerStyle.DestinationChannel(tint.r, spotted), MarkerStyle.DestinationChannel(tint.g, spotted), MarkerStyle.DestinationChannel(tint.b, spotted), 1f);
        }

        private void LateUpdate()
        {
            if (stopped || root == null || image == null || builder == null) return;
            var camera = builder.mapCamera; var controller = TrainController.Shared;
            if (window == null || !window.IsShown || !WaybillMapPlugin.CarsOption.IsOn || camera == null ||
                !camera.isActiveAndEnabled || controller == null || Graph.Shared == null)
            { native.Restore(); root.SetActive(false); nextRefresh = 0; return; }
            root.SetActive(true);
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 0.5f; Refresh(controller); }
            wagons!.Markers.Clear(); engines!.Markers.Clear();
            var rect = image.rectTransform.rect; var uv = image.uvRect; var graph = Graph.Shared.Current;
            var pixelsPerMeter = rect.height / (2 * camera.orthographicSize);
            var scale = MarkerLayout.ScaleForTrack((float)TrackWidth.GetValue(builder) * pixelsPerMeter);
            foreach (var entry in cars)
            {
                var car = entry.Car; if (car == null || !car.LocationF.IsValid || !car.LocationR.IsValid) continue;
                var engine = car is BaseLocomotive;
                if (!MapVisibility.Show(WaybillMapPlugin.VehicleMode, engine, car.CarType.IsCaboose, car == controller.SelectedCar,
                    entry.Index, entry.AverageLength, pixelsPerMeter, scale)) continue;
                try
                {
                    var rear = car.LocationR;
                    var length = car.carLength;
                    if (tenders.TryGetValue(car, out var tender) && tender != null && tender.set != null && car.set != null &&
                        car.CoupledTo(car.EndToLogical(Car.End.R)) == tender)
                    {
                        rear = tender.CoupledTo(tender.EndToLogical(Car.End.F)) == car ? tender.LocationR : tender.LocationF;
                        if (!rear.IsValid) continue;
                        length += tender.carLength;
                    }
                    var a = camera.WorldToViewportPoint(WorldTransformer.GameToWorld(graph.GetPosition(rear)));
                    var b = camera.WorldToViewportPoint(WorldTransformer.GameToWorld(graph.GetPosition(car.LocationF)));
                    if (a.z <= 0 || b.z <= 0) continue;
                    if (MarkerGeometry.TryCreate(a.x, a.y, b.x, b.y, uv.x, uv.y, uv.width, uv.height,
                        rect.xMin, rect.yMin, rect.width, rect.height, out var geometry, length,
                        scale, MarkerLayout.ForConsist(entry.AverageLength, pixelsPerMeter, scale).LengthScale))
                        (engine ? engines : wagons).Markers.Add(new MapMarker(geometry, entry.Color, entry.Label, entry.Number, engine));
                }
                catch (Exception error) { Report(error); }
            }
            wagons.Render(); engines.Render();
            native.Hide(locomotives);
        }

        private void Report(Exception error)
        {
            if (warned) return; warned = true;
            Debug.LogWarning("[WaybillMap] Could not read vehicle map data: " + error);
        }
        internal void Shutdown()
        {
            stopped = true; enabled = false; native.Restore();
            if (root != null) { root.SetActive(false); Object.Destroy(root); root = null; }
            cars.Clear(); locomotives.Clear(); tenders.Clear();
        }
        private void OnDestroy() => Shutdown();
    }
}
