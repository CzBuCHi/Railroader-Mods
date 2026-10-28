using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WaybillMap
{
    internal sealed class VehicleLayer
    {
        internal readonly List<MapMarker> Markers = new List<MapMarker>();
        private readonly RectTransform fills, numbers;
        private readonly List<CarMapGraphic> batches = new List<CarMapGraphic>();
        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();

        internal VehicleLayer(RectTransform parent)
        {
            fills = CreateRect("Bodies", parent); numbers = CreateRect("Numbers", parent);
        }

        internal static RectTransform CreateRect(string name, RectTransform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.pivot = parent.pivot;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal void Render()
        {
            var needed = (Markers.Count + MarkerGeometry.BatchSize - 1) / MarkerGeometry.BatchSize;
            while (batches.Count < needed) batches.Add(CreateRect("Batch", fills).gameObject.AddComponent<CarMapGraphic>());
            for (var i = 0; i < batches.Count; i++)
            {
                batches[i].gameObject.SetActive(i < needed);
                if (i < needed) batches[i].SetMarkers(Markers, i * MarkerGeometry.BatchSize,
                    Math.Min(MarkerGeometry.BatchSize, Markers.Count - i * MarkerGeometry.BatchSize));
            }
            while (labels.Count < Markers.Count)
            {
                var text = new GameObject("VehicleNumber", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                text.rectTransform.SetParent(numbers, false);
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = numbers.pivot;
                text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                text.font = TMP_Settings.defaultFontAsset;
                text.fontSize = 11f; text.fontSizeMin = 4f; text.fontSizeMax = 12f;
                text.lineSpacing = -1f; text.enableAutoSizing = true;
                text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Truncate; text.margin = new Vector4(1f, 0f, 1f, 0f);
                text.raycastTarget = false; text.richText = false;
                labels.Add(text);
            }
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i]; label.gameObject.SetActive(i < Markers.Count);
                if (i >= Markers.Count) continue;
                var marker = Markers[i]; var g = marker.Geometry; var tint = marker.Color;
                label.text = marker.IsLocomotive ? marker.Number : marker.Label;
                label.fontStyle = marker.IsLocomotive ? FontStyles.Bold : FontStyles.Normal;
                label.color = MarkerStyle.UseDarkText(tint.r, tint.g, tint.b, tint.a) ? Color.black : Color.white;
                var inset = marker.IsLocomotive ? 3f : 0f;
                label.rectTransform.anchoredPosition = new Vector2((float)g.X - (float)g.AxisX * inset / 2, (float)g.Y - (float)g.AxisY * inset / 2);
                label.rectTransform.sizeDelta = new Vector2((float)g.HalfLength * 2 - inset, (float)g.HalfWidth * 2);
                label.rectTransform.localRotation = Quaternion.Euler(0, 0, (float)g.ReadableAngle);
            }
        }
    }
}
