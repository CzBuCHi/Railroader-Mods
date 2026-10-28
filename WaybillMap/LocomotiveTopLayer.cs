using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Model;
using TMPro;
using UI.Map;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
{
    // Native locomotive icons are baked into the map texture, below our wagon UI.
    // Mirror their existing graphics in the final UI layer; originals retain clicks.
    internal sealed class LocomotiveTopLayer
    {
        private static readonly FieldInfo IconField = AccessTools.Field(typeof(Car), "MapIcon");
        private readonly RectTransform parent;
        private readonly Dictionary<BaseLocomotive, Copy> copies = new Dictionary<BaseLocomotive, Copy>();
        private readonly HashSet<BaseLocomotive> current = new HashSet<BaseLocomotive>();
        private readonly List<BaseLocomotive> removed = new List<BaseLocomotive>();

        private sealed class Copy
        {
            internal MapIcon Source = null!;
            internal RectTransform SourceRect = null!, Rect = null!;
            internal GameObject Host = null!;
            internal Graphic[] SourceGraphics = null!, Graphics = null!;
        }

        internal LocomotiveTopLayer(RectTransform layer) { parent = layer; }

        internal void Update(IReadOnlyList<BaseLocomotive> locomotives, Camera camera, RawImage mapImage)
        {
            // Keep this above both wagon fills and their road numbers.
            parent.SetAsLastSibling();
            current.Clear();
            foreach (var locomotive in locomotives)
            {
                if (locomotive == null) continue;
                current.Add(locomotive);
                var source = (MapIcon?)IconField.GetValue(locomotive);
                if (copies.TryGetValue(locomotive, out var copy) && copy.Source != source)
                { copy.Host.SetActive(false); Object.Destroy(copy.Host); copies.Remove(locomotive); copy = null; }
                if (source == null || !(source.transform is RectTransform sourceRect))
                { if (copy != null) copy.Host.SetActive(false); continue; }
                if (copy == null)
                {
                    copy = Create(source, sourceRect, mapImage.gameObject.layer);
                    copies.Add(locomotive, copy);
                }
                Position(copy, camera, mapImage);
            }
            removed.Clear();
            foreach (var pair in copies)
                if (pair.Key == null || !current.Contains(pair.Key)) removed.Add(pair.Key!);
            foreach (var car in removed)
            { copies[car].Host.SetActive(false); Object.Destroy(copies[car].Host); copies.Remove(car); }
        }

        private Copy Create(MapIcon source, RectTransform sourceRect, int layer)
        {
            var host = new GameObject("LocomotiveTop", typeof(RectTransform));
            host.SetActive(false); // Prevent cloned MapIcon.OnEnable from registering it with the world map.
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.SetParent(parent, false);
            hostRect.pivot = parent.pivot;
            hostRect.anchorMin = Vector2.zero; hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = hostRect.offsetMax = Vector2.zero;
            var clone = Object.Instantiate(source.gameObject, host.transform, false);
            foreach (var icon in clone.GetComponentsInChildren<MapIcon>(true))
            { icon.enabled = false; Object.Destroy(icon); }
            foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var raycaster in clone.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
            foreach (var canvas in clone.GetComponentsInChildren<Canvas>(true)) canvas.overrideSorting = false;
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = layer;
            var graphics = clone.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics) graphic.raycastTarget = false;
            var rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = parent.pivot;
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = sourceRect.rect.size;
            return new Copy { Source = source, SourceRect = sourceRect, Rect = rect, Host = host,
                SourceGraphics = source.GetComponentsInChildren<Graphic>(true), Graphics = graphics };
        }

        private void Position(Copy copy, Camera camera, RawImage image)
        {
            var uv = image.uvRect; var bounds = image.rectTransform.rect;
            if (!copy.Source.isActiveAndEnabled || Math.Abs(uv.width) < 1e-6 || Math.Abs(uv.height) < 1e-6)
            { copy.Host.SetActive(false); return; }
            var source = copy.SourceRect;
            var origin = Project(camera, source.position, uv, bounds);
            var b = Project(camera, source.TransformPoint(Vector3.right), uv, bounds);
            var c = Project(camera, source.TransformPoint(Vector3.up), uv, bounds);
            if (origin.z <= 0f)
            { copy.Host.SetActive(false); return; }
            var along = (Vector2)(b - origin); var across = (Vector2)(c - origin);
            copy.Rect.anchoredPosition = origin;
            copy.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
            var cross = along.x * across.y - along.y * across.x;
            copy.Rect.localScale = new Vector3(along.magnitude,
                across.magnitude * (cross < 0f ? -1f : 1f), 1f);
            for (var i = 0; i < Math.Min(copy.Graphics.Length, copy.SourceGraphics.Length); i++)
            {
                var original = copy.SourceGraphics[i]; var graphic = copy.Graphics[i];
                if (original == null || graphic == null) continue;
                graphic.color = original.color;
                // Root geometry is projected above; only child text rotations are copied.
                if (graphic.transform != copy.Rect) graphic.rectTransform.localRotation = original.rectTransform.localRotation;
                if (original is TMP_Text originalText && graphic is TMP_Text text) text.text = originalText.text;
            }
            copy.Host.SetActive(true);
        }

        private static Vector3 Project(Camera camera, Vector3 world, Rect uv, Rect bounds)
        {
            var point = camera.WorldToViewportPoint(world);
            return new Vector3(bounds.xMin + (point.x - uv.x) / uv.width * bounds.width,
                bounds.yMin + (point.y - uv.y) / uv.height * bounds.height, point.z);
        }

        internal void Clear()
        {
            foreach (var copy in copies.Values)
            { copy.Host.SetActive(false); Object.Destroy(copy.Host); }
            copies.Clear(); current.Clear(); removed.Clear();
        }
    }
}
