using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Helpers;
using Model;
using TMPro;
using UI.Map;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
{
    // Native locomotive icons are baked into the map texture, below our wagon UI.
    // Draw compact copies last and temporarily hide native graphics; originals retain clicks.
    internal sealed class LocomotiveTopLayer
    {
        private static readonly FieldInfo IconField = AccessTools.Field(typeof(Car), "MapIcon");
        private readonly RectTransform parent;
        private readonly Dictionary<BaseLocomotive, Copy> copies = new Dictionary<BaseLocomotive, Copy>();
        private readonly HashSet<BaseLocomotive> current = new HashSet<BaseLocomotive>();
        private readonly List<BaseLocomotive> removed = new List<BaseLocomotive>();
        private readonly Vector3[] corners = new Vector3[4];

        private sealed class Copy
        {
            internal MapIcon Source = null!;
            internal RectTransform SourceRect = null!, Rect = null!;
            internal GameObject Host = null!;
            internal Graphic[] SourceGraphics = null!, Graphics = null!;
            internal bool[] EnabledBeforeHide = null!;
            internal bool Hidden;
            internal RectTransform? Body;
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
                { Destroy(copy); copies.Remove(locomotive); copy = null; }
                if (source == null || !(source.transform is RectTransform sourceRect))
                { if (copy != null) { Restore(copy); copy.Host.SetActive(false); } continue; }
                if (copy == null)
                {
                    copy = Create(source, sourceRect, mapImage.gameObject.layer);
                    copies.Add(locomotive, copy);
                }
                try
                {
                    if (Position(copy, locomotive, camera, mapImage)) Hide(copy);
                    else Restore(copy);
                }
                catch { Restore(copy); copy.Host.SetActive(false); throw; }
            }
            removed.Clear();
            foreach (var pair in copies)
                if (pair.Key == null || !current.Contains(pair.Key)) removed.Add(pair.Key!);
            foreach (var car in removed)
            { Destroy(copies[car]); copies.Remove(car); }
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
            var originals = source.GetComponentsInChildren<Graphic>(true);
            RectTransform? body = null;
            foreach (var graphic in originals) if (graphic is Image) { body = graphic.rectTransform; break; }
            return new Copy { Source = source, SourceRect = sourceRect, Rect = rect, Host = host,
                SourceGraphics = originals, Graphics = graphics, EnabledBeforeHide = new bool[originals.Length], Body = body };
        }

        private bool Position(Copy copy, BaseLocomotive locomotive, Camera camera, RawImage image)
        {
            var uv = image.uvRect; var bounds = image.rectTransform.rect;
            if (!copy.Source.isActiveAndEnabled || Math.Abs(uv.width) < 1e-6 || Math.Abs(uv.height) < 1e-6)
            { copy.Host.SetActive(false); return false; }
            var source = copy.SourceRect;
            var origin = Project(camera, source.position, uv, bounds);
            var b = Project(camera, source.TransformPoint(Vector3.right), uv, bounds);
            var c = Project(camera, source.TransformPoint(Vector3.up), uv, bounds);
            if (origin.z <= 0f)
            { copy.Host.SetActive(false); return false; }
            var along = (Vector2)(b - origin); var across = (Vector2)(c - origin);
            if (copy.Body == null || !locomotive.LocationF.IsValid || !locomotive.LocationR.IsValid)
            { copy.Host.SetActive(false); return false; }
            copy.Body.GetWorldCorners(corners);
            var p0 = Project(camera, corners[0], uv, bounds);
            var p1 = Project(camera, corners[1], uv, bounds);
            var p2 = Project(camera, corners[2], uv, bounds);
            var nativeLength = Math.Max(((Vector2)(p1 - p0)).magnitude, ((Vector2)(p2 - p1)).magnitude);
            var graph = Track.Graph.Shared.Current;
            locomotive.GetPositionFR(graph, out var front, out var rear);
            var projectedFront = Project(camera, WorldTransformer.GameToWorld(front), uv, bounds);
            var projectedRear = Project(camera, WorldTransformer.GameToWorld(rear), uv, bounds);
            var fit = (float)MarkerStyle.LocomotiveScale(((Vector2)(projectedFront - projectedRear)).magnitude, nativeLength);
            if (fit <= 0f) { copy.Host.SetActive(false); return false; }
            copy.Rect.anchoredPosition = origin;
            copy.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
            var cross = along.x * across.y - along.y * across.x;
            copy.Rect.localScale = new Vector3(along.magnitude * fit,
                across.magnitude * fit * (cross < 0f ? -1f : 1f), 1f);
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
            return true;
        }

        private static void Hide(Copy copy)
        {
            if (copy.Hidden) return;
            for (var i = 0; i < copy.SourceGraphics.Length; i++)
            {
                var graphic = copy.SourceGraphics[i];
                if (graphic == null) continue;
                copy.EnabledBeforeHide[i] = graphic.enabled;
                graphic.enabled = false;
            }
            copy.Hidden = true;
        }

        private static void Restore(Copy copy)
        {
            if (!copy.Hidden) return;
            for (var i = 0; i < copy.SourceGraphics.Length; i++)
                if (copy.SourceGraphics[i] != null) copy.SourceGraphics[i].enabled = copy.EnabledBeforeHide[i];
            copy.Hidden = false;
        }

        private static void Destroy(Copy copy)
        { Restore(copy); copy.Host.SetActive(false); Object.Destroy(copy.Host); }

        internal void Suspend()
        {
            foreach (var copy in copies.Values) { Restore(copy); copy.Host.SetActive(false); }
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
                Destroy(copy);
            copies.Clear(); current.Clear(); removed.Clear();
        }
    }
}
