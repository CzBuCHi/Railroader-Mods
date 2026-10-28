using System;
using System.Collections.Generic;
using Helpers;
using Model;
using Model.Ops;
using Track;
using TMPro;
using UI.Map;
using UI.Tags;
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
        private RectTransform? markerLayer, labelLayer;
        private bool stopped, warned;
        private float nextRefresh;
        private readonly List<(Car Car, Color Color, string Number)> cars = new List<(Car, Color, string)>();
        private readonly List<MapMarker> markers = new List<MapMarker>();
        private readonly List<CarMapGraphic> batches = new List<CarMapGraphic>();
        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();

        internal void Initialize(RawImage mapImage, MapBuilder mapBuilder)
        {
            image = mapImage; builder = mapBuilder; window = GetComponent<Window>();
            nextRefresh = 0f;
            if (root != null) return;
            root = new GameObject("CzBuCHi.WaybillMap.Clip", typeof(RectTransform), typeof(RectMask2D));
            Stretch(root.GetComponent<RectTransform>(), image.rectTransform);
            markerLayer = CreateLayer("Markers");
            labelLayer = CreateLayer("Numbers");
            root.SetActive(false);
        }

        private RectTransform CreateLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            Stretch(layer, root!.transform);
            return layer;
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            if (parent is RectTransform parentRect) rect.pivot = parentRect.pivot;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (stopped || root == null || image == null || builder == null) return;
            var camera = builder.mapCamera;
            var controller = TrainController.Shared;
            if (window == null || !window.IsShown || !WaybillMapPlugin.CarsOption.IsOn ||
                camera == null || !camera.isActiveAndEnabled || controller == null || Graph.Shared == null)
            {
                root.SetActive(false); nextRefresh = 0f;
                return;
            }
            root.SetActive(true);
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.5f;
                cars.Clear();
                // The logical registry includes distant cars without loaded detailed models.
                foreach (var car in controller.Cars)
                {
                    if (car == null || car is BaseLocomotive) continue;
                    try
                    {
                        var channel = MarkerStyle.UnassignedChannel;
                        Color tint = car.Waybill.HasValue ? CarTagColor.For(car, OpsController.Shared) : new Color32(channel, channel, channel, 255);
                        cars.Add((car, tint, car.Ident.RoadNumber));
                    }
                    catch (Exception error) { Report(error); }
                }
            }
            markers.Clear();
            var rect = image.rectTransform.rect;
            var uv = image.uvRect;
            var graph = Graph.Shared.Current;
            foreach (var entry in cars)
            {
                var car = entry.Car;
                if (car == null || !car.LocationA.IsValid || !car.LocationB.IsValid) continue;
                try
                {
                    var a = camera.WorldToViewportPoint(WorldTransformer.GameToWorld(graph.GetPosition(car.LocationA)));
                    var b = camera.WorldToViewportPoint(WorldTransformer.GameToWorld(graph.GetPosition(car.LocationB)));
                    if (a.z <= 0f || b.z <= 0f) continue;
                    if (MarkerGeometry.TryCreate(a.x, a.y, b.x, b.y, uv.x, uv.y, uv.width, uv.height,
                        rect.xMin, rect.yMin, rect.width, rect.height, out var geometry))
                        markers.Add(new MapMarker(geometry, entry.Color, entry.Number));
                }
                catch (Exception error) { Report(error); }
            }
            var needed = (markers.Count + MarkerGeometry.BatchSize - 1) / MarkerGeometry.BatchSize;
            while (batches.Count < needed)
            {
                var batch = new GameObject("Cars", typeof(RectTransform));
                Stretch(batch.GetComponent<RectTransform>(), markerLayer!);
                batches.Add(batch.AddComponent<CarMapGraphic>());
            }
            for (var i = 0; i < batches.Count; i++)
            {
                batches[i].gameObject.SetActive(i < needed);
                if (i < needed) batches[i].SetMarkers(markers, i * MarkerGeometry.BatchSize,
                    Math.Min(MarkerGeometry.BatchSize, markers.Count - i * MarkerGeometry.BatchSize));
            }
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            while (labels.Count < markers.Count)
            {
                var text = new GameObject("CarNumber", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                var rect = text.rectTransform;
                rect.SetParent(labelLayer, false);
                rect.anchorMin = rect.anchorMax = labelLayer!.pivot;
                rect.pivot = new Vector2(0.5f, 0.5f);
                text.font = TMP_Settings.defaultFontAsset;
                text.fontSize = 11f; text.fontSizeMin = 8f; text.fontSizeMax = 12f;
                text.enableAutoSizing = true;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Truncate;
                text.margin = new Vector4(2f, 0f, 2f, 0f);
                text.raycastTarget = false; text.richText = false;
                labels.Add(text);
            }
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                label.gameObject.SetActive(i < markers.Count);
                if (i >= markers.Count) continue;
                var marker = markers[i]; var geometry = marker.Geometry; var tint = marker.Color;
                label.text = marker.Number;
                label.color = MarkerStyle.UseDarkText(tint.r, tint.g, tint.b, tint.a) ? Color.black : Color.white;
                var rect = label.rectTransform;
                rect.anchoredPosition = new Vector2((float)geometry.X, (float)geometry.Y);
                rect.sizeDelta = new Vector2((float)geometry.HalfLength * 2f, (float)geometry.HalfWidth * 2f);
                rect.localRotation = Quaternion.Euler(0f, 0f, (float)geometry.ReadableAngle);
            }
        }

        private void Report(Exception error)
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("[WaybillMap] Skipped a car whose map data could not be read: " + error);
        }

        internal void Shutdown()
        {
            stopped = true; enabled = false;
            if (root != null) { root.SetActive(false); Object.Destroy(root); root = null; }
            cars.Clear(); markers.Clear(); batches.Clear(); labels.Clear();
        }
        private void OnDestroy() => Shutdown();
    }
}
