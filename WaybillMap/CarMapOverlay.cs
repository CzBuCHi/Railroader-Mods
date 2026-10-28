using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Helpers;
using Model;
using Model.Ops;
using TMPro;
using Track;
using UI.Map;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
{
    // MapEnhancer's native world-space MapIcon pipeline, adapted to 0.6's prefab contract.
    [DefaultExecutionOrder(1100)]
    internal sealed class CarMapOverlay : MonoBehaviour
    {
        private static readonly System.Reflection.FieldInfo Prefabs = AccessTools.Field(typeof(Car), "_setupPrefabs");
        private static readonly System.Reflection.PropertyInfo IconScale = AccessTools.Property(typeof(MapBuilder), "IconScale");
        private MapBuilder? builder;
        private Window? window;
        private GameObject? root, construction;
        private Texture2D? texture;
        private Sprite? wagonSprite;
        private float nextRefresh;
        private bool stopped, warned;
        private readonly NativeMapIcons native = new NativeMapIcons();
        private readonly List<BaseLocomotive> locomotives = new List<BaseLocomotive>();
        private readonly List<Entry> cars = new List<Entry>();
        private readonly Dictionary<Car, Marker> markers = new Dictionary<Car, Marker>();
        private Dictionary<Car, Car> tenders = new Dictionary<Car, Car>();
        private readonly HashSet<Car> retained = new HashSet<Car>();
        private readonly List<Car> removed = new List<Car>();
        private static readonly Color Unassigned = new Color32(MarkerStyle.UnassignedChannel, MarkerStyle.UnassignedChannel, MarkerStyle.UnassignedChannel, 255);
        private sealed class Entry
        {
            internal Car Car = null!;
            internal Color Color;
            internal string Label = "";
            internal int Index;
        }
        private sealed class Marker
        {
            internal MapIcon Icon = null!;
            internal Image Body = null!;
            internal TMP_Text Text = null!;
            internal Canvas Canvas = null!;
            internal VehicleMapTag Tag = null!;
            internal float Zoom = float.NaN;
            internal string? LastLabel;
        }

        internal void Initialize(RawImage _, MapBuilder mapBuilder)
        {
            builder = mapBuilder; window = GetComponent<Window>(); nextRefresh = 0;
            if (root != null) return;
            root = new GameObject("CzBuCHi.WaybillMap.WorldIcons") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(builder.transform, false); root.SetActive(false);
            construction = new GameObject("Creating icons") { hideFlags = HideFlags.DontSave };
            construction.transform.SetParent(root.transform, false); construction.SetActive(false);
            using var stream = typeof(CarMapOverlay).Assembly.GetManifestResourceStream("WaybillMap.traincar.png")
                ?? throw new InvalidDataException("Embedded MapEnhancer wagon sprite is missing.");
            using var bytes = new MemoryStream(); stream.CopyTo(bytes);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            if (!texture.LoadImage(bytes.ToArray())) throw new InvalidDataException("Cannot decode wagon sprite.");
            wagonSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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
                        Label = MapEnhancerStyle.Label(car.Ident.ReportingMark, car.Ident.RoadNumber, car is BaseLocomotive || car.CarType.IsTender) }); }
                    catch (Exception error) { Report(error); }
                }
            }
            retained.Clear(); foreach (var entry in cars) retained.Add(entry.Car);
            removed.Clear(); foreach (var pair in markers) if (pair.Key == null || !retained.Contains(pair.Key)) removed.Add(pair.Key!);
            foreach (var car in removed) { DestroyMarker(markers[car]); markers.Remove(car); }
        }

        private static Color ColorFor(Car car)
        {
            var ops = OpsController.Shared;
            if (ops == null || !ops.TryGetDestinationInfo(car, out _, out var spotted, out _, out var destination)) return Unassigned;
            var tint = ops.AreaForCarPosition(destination)?.tagColor ?? Unassigned;
            // Area colors may already be stored dark. Match MapEnhancer's bright
            // unspotted palette first, then apply only a mild spotted distinction.
            var brightness = MarkerStyle.DestinationBrightness(tint.r, tint.g, tint.b, spotted);
            return brightness == 0 ? Unassigned : new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, 1);
        }

        private Marker Create(Car car)
        {
            var prefab = ((Car.SetupPrefabs)Prefabs.GetValue(car)).LocomotiveMapIcon;
            if (prefab == null) throw new InvalidOperationException("Native locomotive map prefab is not ready.");
            var icon = Object.Instantiate(prefab, construction!.transform, false);
            icon.name = "WaybillMap Vehicle";
            var body = icon.GetComponentInChildren<Image>(true);
            var text = icon.GetComponentInChildren<TMP_Text>(true);
            if (body == null || text == null) { Object.Destroy(icon.gameObject); throw new InvalidOperationException("Native map icon layout changed."); }
            var tag = icon.gameObject.AddComponent<VehicleMapTag>(); tag.Text = text;
            if (!(car is BaseLocomotive)) body.sprite = wagonSprite;
            // 0.6's locomotive prefab can constrain/mask its numeric caption. Wagon
            // labels must be independent two-line blocks, as in the old prefab.
            if (text.transform != icon.transform) text.transform.SetParent(icon.transform, false);
            body.transform.localScale = Vector3.one * MapEnhancerStyle.ChildScale;
            text.transform.localScale = Vector3.one * MapEnhancerStyle.ChildScale;
            if (!(car is BaseLocomotive))
            {
                // Copied from MapEnhancer.AddTraincarMarker.
                text.horizontalAlignment = HorizontalAlignmentOptions.Left;
                text.enableAutoSizing = false; text.fontSizeMin = MapEnhancerStyle.FontSize; text.fontSize = MapEnhancerStyle.FontSize;
                text.autoSizeTextContainer = true; text.transform.localPosition = Vector3.zero;
            }
            text.richText = true;
            text.margin = Vector4.zero; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow; text.maskable = false;
            text.maxVisibleCharacters = text.maxVisibleWords = text.maxVisibleLines = int.MaxValue;
            text.firstVisibleCharacter = 0;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            foreach (var collider in icon.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var graphic in icon.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            icon.OnClick = null;
            // Awake/OnEnable now run with the own-marker tag and native Canvas already configured.
            icon.transform.SetParent(root!.transform, false);
            return new Marker { Icon = icon, Body = body, Text = text, Canvas = icon.GetComponent<Canvas>(), Tag = tag };
        }

        private void LateUpdate()
        {
            if (stopped || root == null || builder == null) return;
            var camera = builder.mapCamera; var controller = TrainController.Shared;
            if (window == null || !window.IsShown || !WaybillMapPlugin.CarsOption.IsOn || camera == null ||
                !camera.isActiveAndEnabled || controller == null || Graph.Shared == null)
            { native.Restore(); root.SetActive(false); nextRefresh = 0; return; }
            root.SetActive(true);
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 0.5f; Refresh(controller); }
            var graph = Graph.Shared.Current;
            var zoom = (float)IconScale.GetValue(builder);
            foreach (var entry in cars)
            {
                var car = entry.Car;
                if (car == null) continue;
                try
                {
                    if (!markers.TryGetValue(car, out var marker)) { marker = Create(car); markers.Add(car, marker); }
                    var engine = car is BaseLocomotive;
                    var stride = MapEnhancerStyle.SamplingStride(marker.Body.rectTransform.rect.height, zoom);
                    if (!car.LocationF.IsValid || !car.LocationR.IsValid || !MapVisibility.Show(WaybillMapPlugin.VehicleMode,
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
                    // MapEnhancer uses separate 4000/3000 altitude layers. Anchor those
                    // layers to the camera so high terrain cannot place an icon above it.
                    center.y = camera.transform.position.y - (engine ? 1000f : 2000f);
                    var rotation = car.GetCenterRotation(graph);
                    marker.Icon.transform.SetPositionAndRotation(center, Quaternion.Euler(-90, rotation.eulerAngles.y, 0));
                    marker.Body.transform.localScale = new Vector3(MapEnhancerStyle.ChildScale,
                        engine ? MapEnhancerStyle.EngineScaleY(car.carLength, length) : MapEnhancerStyle.WagonScaleY(length), MapEnhancerStyle.ChildScale);
                    marker.Body.color = entry.Color;
                    marker.Text.color = Color.black;
                    if (marker.LastLabel != entry.Label)
                    {
                        marker.Icon.SetText(entry.Label);
                        if (!engine)
                        {
                            marker.Text.rectTransform.sizeDelta = marker.Text.GetPreferredValues(entry.Label);
                            marker.Text.transform.localPosition = Vector3.zero;
                        }
                        marker.LastLabel = entry.Label;
                    }
                    marker.Canvas.enabled = true;
                    if (marker.Zoom != zoom) { marker.Icon.SetZoom(zoom); marker.Zoom = zoom; }
                    marker.Tag.FixRotation();
                }
                catch (Exception error) { Report(error); }
            }
            native.Hide(locomotives);
        }

        private static void DestroyMarker(Marker marker)
        { if (marker.Icon != null) { marker.Icon.gameObject.SetActive(false); Object.Destroy(marker.Icon.gameObject); } }
        private void Report(Exception error)
        { if (!warned) { warned = true; Debug.LogWarning("[WaybillMap] Could not render vehicle: " + error); } }
        internal void Shutdown()
        {
            stopped = true; enabled = false; native.Restore();
            foreach (var marker in markers.Values) DestroyMarker(marker); markers.Clear();
            if (root != null) { root.SetActive(false); Object.Destroy(root); root = null; }
            if (wagonSprite != null) { Object.Destroy(wagonSprite); wagonSprite = null; }
            if (texture != null) { Object.Destroy(texture); texture = null; }
            cars.Clear(); locomotives.Clear(); tenders.Clear();
        }
        private void OnDestroy() => Shutdown();
    }
}
