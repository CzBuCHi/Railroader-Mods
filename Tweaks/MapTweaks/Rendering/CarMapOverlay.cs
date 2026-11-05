using System.Collections.Generic;
using System.Linq;
using System;
using HarmonyLib;
using Helpers;
using Model.Ops;
using Model;
using Track;
using Tweaks.MapTweaks.Configuration;
using Tweaks.MapTweaks;
using UI.Map;
using UI.Windows;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks.MapTweaks.Rendering
{
    [DefaultExecutionOrder(1100)]
    internal sealed class CarMapOverlay : MonoBehaviour
    {
        private static readonly System.Reflection.PropertyInfo IconScale = AccessTools.Property(typeof(MapBuilder), "IconScale");
        private MapBuilder? builder;
        private Window? window;
        private GameObject? root;
        private float nextRefresh;
        private bool stopped, warned;
        private readonly WagonSprite wagonSprite = new WagonSprite();
        private readonly NativeMapIcons native = new NativeMapIcons();
        private readonly List<BaseLocomotive> locomotives = new List<BaseLocomotive>();
        private readonly List<Entry> cars = new List<Entry>();
        private readonly Dictionary<Car, VehicleMarker> markers = new Dictionary<Car, VehicleMarker>();
        private Dictionary<Car, Car> tenders = new Dictionary<Car, Car>();
        private readonly HashSet<Car> retained = new HashSet<Car>();
        private readonly List<Car> removed = new List<Car>();
        private static readonly Color Unassigned = new Color32(221, 221, 221, 255);
        private sealed class Entry
        {
            internal Car Car = null!;
            internal Color Color;
            internal string Label = "";
            internal int Index;
        }
        internal void Initialize(RawImage _, MapBuilder mapBuilder)
        {
            builder = mapBuilder; window = GetComponent<Window>(); nextRefresh = 0;
            if (root != null) return;
            root = new GameObject("CzBuCHi.MapTweaks.WorldIcons") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(builder.transform, false); root.SetActive(false);
        }

        private void Refresh(TrainController controller)
        {
            cars.Clear(); locomotives.Clear();
            var registered = controller.Cars.Where(c => c != null).OrderBy(c => c.id, StringComparer.Ordinal).ToArray();
            tenders = TenderPairing.Find(registered, c => c is SteamLocomotive steam && steam.hasTender,
                c => c.set == null ? null : c.CoupledTo(c.EndToLogical(Car.End.R)), c => c.CarType.IsTender);
            var merged = new HashSet<Car>(tenders.Values); var seen = new HashSet<Car>();
            foreach (var anchor in registered)
            {
                if (seen.Contains(anchor)) continue;
                var group = anchor.set == null ? new[] { anchor } : anchor.EnumerateCoupled().Where(c => c != null).ToArray();
                if (group.Length == 0) group = new[] { anchor };
                var index = 0;
                foreach (var car in group)
                {
                    if (!seen.Add(car) || merged.Contains(car) || car.IsInBardo) continue;
                    if (car is BaseLocomotive engine) locomotives.Add(engine);
                    try { cars.Add(new Entry { Car = car, Color = ColorFor(car), Index = index++,
                        Label = VehicleMarkerGeometry.Label(car is BaseLocomotive, car.Ident.RoadNumber) }); }
                    catch (Exception error) { Report(error); }
                }
            }
            retained.Clear(); foreach (var entry in cars) retained.Add(entry.Car);
            removed.Clear(); foreach (var pair in markers) if (pair.Key == null || !retained.Contains(pair.Key)) removed.Add(pair.Key!);
            foreach (var car in removed) { markers[car].Destroy(); markers.Remove(car); }
        }

        private static Color ColorFor(Car car)
        {
            var ops = OpsController.Shared;
            if (ops == null || !ops.TryGetDestinationInfo(car, out _, out var spotted, out _, out var destination)) return Unassigned;
            var tint = ops.AreaForCarPosition(destination)?.tagColor ?? Unassigned;
            // Normalize dark area colors before applying the spotted distinction.
            var peak = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b));
            var brightness = peak <= 0.0001f ? 0 : (spotted ? 0.6f : 1f) / peak;
            return brightness == 0 ? Unassigned : new Color(
                DestinationChannel(tint.r, brightness, spotted),
                DestinationChannel(tint.g, brightness, spotted),
                DestinationChannel(tint.b, brightness, spotted), 1);
        }

        private static float DestinationChannel(float channel, float brightness, bool spotted) =>
            spotted ? channel * brightness : channel * brightness * 0.8f + 0.2f;

        private static bool ShouldShow(VehicleDisplayMode mode, bool engine, bool caboose, bool selected, int index, int stride)
        {
            if (engine) return true;
            if (mode == VehicleDisplayMode.LocomotivesOnly) return false;
            if (mode == VehicleDisplayMode.LocomotivesAndCabooses) return caboose;
            return caboose || selected || index % Math.Max(1, stride) == 0;
        }

        internal Car? CarAt(Ray ray)
        {
            if (root == null || !root.activeInHierarchy) return null;
            Car? result = null; var best = float.PositiveInfinity; var engineHit = false;
            foreach (var pair in markers)
            {
                var marker = pair.Value;
                if (pair.Key == null || !marker.Canvas.enabled) continue;
                var rect = marker.Body.rectTransform;
                if (!new Plane(rect.forward, rect.position).Raycast(ray, out var distance)) continue;
                var local = rect.InverseTransformPoint(ray.GetPoint(distance));
                if (!rect.rect.Contains(new Vector2(local.x, local.y))) continue;
                var engine = pair.Key is BaseLocomotive;
                var score = local.sqrMagnitude;
                if ((engine && !engineHit) || (engine == engineHit && score < best))
                { result = pair.Key; best = score; engineHit = engine; }
            }
            return result;
        }

        private void LateUpdate()
        {
            if (stopped || root == null || builder == null) return;
            var camera = builder.mapCamera; var controller = TrainController.Shared;
            if (window == null || !window.IsShown || camera == null ||
                !camera.isActiveAndEnabled || controller == null || Graph.Shared == null)
            { native.Restore(); root.SetActive(false); nextRefresh = 0; return; }
            root.SetActive(true);
            if (UnityEngine.Time.unscaledTime >= nextRefresh) { nextRefresh = UnityEngine.Time.unscaledTime + 0.5f; Refresh(controller); }
            var graph = Graph.Shared.Current;
            var zoom = (float)IconScale.GetValue(builder);
            var metersPerPixel = 2f * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight);
            foreach (var entry in cars)
            {
                var car = entry.Car;
                if (car == null) continue;
                try
                {
                    if (!markers.TryGetValue(car, out var marker)) { marker = VehicleMarker.Create(car, root.transform, camera, wagonSprite); markers.Add(car, marker); }
                    var engine = car is BaseLocomotive;
                    var stride = VehicleMarkerGeometry.SamplingStride(car.carLength, metersPerPixel, zoom);
                    if (!car.LocationF.IsValid || !car.LocationR.IsValid || !ShouldShow(MapTweaksFeature.VehicleDisplayMode,
                        engine, car.CarType.IsCaboose, car == controller.SelectedCar, entry.Index, stride))
                    { marker.Canvas.enabled = false; continue; }
                    var rear = car.LocationR; var length = car.carLength;
                    if (tenders.TryGetValue(car, out var tender) && tender != null && tender.set != null && car.set != null &&
                        car.CoupledTo(car.EndToLogical(Car.End.R)) == tender)
                    {
                        rear = tender.CoupledTo(tender.EndToLogical(Car.End.F)) == car ? tender.LocationR : tender.LocationF;
                        if (!rear.IsValid) { marker.Canvas.enabled = false; continue; }
                        length += tender.carLength;
                    }
                    var frontPoint = graph.GetPosition(car.LocationF); var rearPoint = graph.GetPosition(rear);
                    var center = WorldTransformer.GameToWorld(Vector3.Lerp(frontPoint, rearPoint, 0.5f));
                    // Reserve two planes inside the map camera's clipping range.
                    center.y = camera.transform.position.y - Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, engine ? 0.25f : 0.5f);
                    var rotation = car.GetCenterRotation(graph);
                    marker.Root.transform.SetPositionAndRotation(center, Quaternion.Euler(-90, rotation.eulerAngles.y, 0));
                    marker.UpdateAppearance(entry.Label, entry.Color, length, car.carLength, zoom, metersPerPixel);
                    marker.Canvas.enabled = true;
                }
                catch (Exception error) { Report(error); }
            }
            native.Hide(locomotives);
        }

        private void Report(Exception error)
        { if (!warned) { warned = true; Debug.LogWarning("[MapTweaks] Could not render vehicle: " + error); } }
        internal void Shutdown()
        {
            stopped = true; enabled = false; native.Restore();
            foreach (var marker in markers.Values) marker.Destroy(); markers.Clear();
            if (root != null) { root.SetActive(false); Object.Destroy(root); root = null; }
            wagonSprite.Dispose();
            cars.Clear(); locomotives.Clear(); tenders.Clear();
        }
        private void OnDestroy() => Shutdown();
    }
}
