using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace Tweaks.Hud
{
    internal sealed class HudButtonStyle
    {
        private readonly Button template;
        private readonly Image frame;
        internal readonly float Width, Height, Gap;
        internal readonly float? RowLeft;
        internal Texture? PaletteTexture { get; private set; }
        internal Vector2 PaletteUV { get; private set; }
        internal Color GlyphTint => frame.color * template.colors.normalColor * template.colors.colorMultiplier;
        internal Material Material => frame.material;

        private HudButtonStyle(Button source, RectTransform coordinates, IEnumerable<Button> candidates, float fallbackSize = 0)
        {
            template = source; frame = (Image)source.targetGraphic;
            var bounds = Bounds(frame.rectTransform, coordinates);
            Width = fallbackSize > 0 ? fallbackSize : bounds.width; Height = fallbackSize > 0 ? fallbackSize : bounds.height;
            var row = candidates
                .Select(b => Bounds(b.targetGraphic.rectTransform, coordinates))
                .Where(r => Math.Abs(r.center.y - bounds.center.y) < Height * 0.25f && Math.Abs(r.center.x - bounds.center.x) < Width * 4)
                .OrderBy(r => r.xMin).ToArray();
            var gaps = new List<float>();
            for (var i = 1; i < row.Length; i++)
            {
                var gap = row[i].xMin - row[i - 1].xMax;
                if (gap >= 0 && gap <= Width * 0.6f) gaps.Add(gap);
            }
            Gap = gaps.Count > 0 ? gaps.OrderBy(g => g).ElementAt(gaps.Count / 2) : 4f;
            if (row.Length > 1) RowLeft = row[0].xMin;
            ReadPalettePixel();
        }

        internal static HudButtonStyle Find(RectTransform dropdown, Button fallback)
        {
            var coordinates = (RectTransform)dropdown.parent;
            var reference = Bounds(dropdown, coordinates).center;
            for (var scope = dropdown.parent; scope != null; scope = scope.parent)
            {
                var candidates = scope.GetComponentsInChildren<Button>(true).Where(b => b.gameObject.activeInHierarchy &&
                    !b.transform.IsChildOf(dropdown) && b.targetGraphic is Image && !IsOurButton(b))
                    .Where(b => { var r = Bounds(b.GetComponent<RectTransform>(), coordinates); return r.height > 5 && r.width / r.height >= 0.65f && r.width / r.height <= 1.8f; })
                    .ToArray();
                if (candidates.Length >= 2)
                {
                    var nearest = candidates.OrderBy(b => (Bounds(b.GetComponent<RectTransform>(), coordinates).center - reference).sqrMagnitude).First();
                    return new HudButtonStyle(nearest, coordinates, candidates);
                }
                if (scope.GetComponent<Canvas>() != null) break;
            }
            return new HudButtonStyle(fallback, coordinates, Array.Empty<Button>(), Math.Max(16, Bounds(dropdown, coordinates).height));
        }

        private static bool IsOurButton(Button button)
        {
            for (var parent = button.transform.parent; parent != null; parent = parent.parent)
                if (parent.name == "CzBuCHi.Tweaks.ControlModes") return true;
            return false;
        }

        internal static Rect Bounds(RectTransform rect, RectTransform coordinates)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var a = coordinates.InverseTransformPoint(corners[0]); var b = coordinates.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        }

        internal void Apply(Button button)
        {
            var image = (Image)button.targetGraphic;
            image.sprite = frame.sprite; image.overrideSprite = null;
            image.color = frame.color; image.material = frame.material;
            image.type = frame.type; image.preserveAspect = frame.preserveAspect;
            image.fillCenter = frame.fillCenter; image.pixelsPerUnitMultiplier = frame.pixelsPerUnitMultiplier;
            button.transition = template.transition; button.colors = template.colors; button.spriteState = template.spriteState;
        }

        private void ReadPalettePixel()
        {
            if (frame.sprite == null || frame.sprite.texture == null) return;
            RenderTexture? temporary = null; Texture2D? scratch = null;
            var previous = RenderTexture.active;
            try
            {
                var atlas = frame.sprite.texture; var rect = frame.sprite.textureRect;
                var width = Mathf.RoundToInt(rect.width); var height = Mathf.RoundToInt(rect.height);
                temporary = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(atlas, temporary); RenderTexture.active = temporary;
                scratch = new Texture2D(width, height, TextureFormat.RGBA32, false);
                scratch.ReadPixels(rect, 0, 0); scratch.Apply(false, false);
                var pixels = scratch.GetPixels32(); var alpha = pixels.Max(p => p.a);
                if (alpha == 0) return;
                var brightest = pixels.Where(p => p.a >= alpha - 2).Max(p => p.r + p.g + p.b);
                var counts = new Dictionary<uint, int>(); var first = new Dictionary<uint, int>();
                for (var i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.a < alpha - 2 || p.r + p.g + p.b < brightest * 0.35f) continue;
                    var key = ((uint)p.r << 16) | ((uint)p.g << 8) | p.b;
                    if (!counts.ContainsKey(key)) { counts[key] = 0; first[key] = i; }
                    counts[key]++;
                }
                var index = first[counts.OrderByDescending(p => p.Value).First().Key];
                PaletteTexture = atlas;
                PaletteUV = new Vector2((rect.x + index % width + 0.5f) / atlas.width, (rect.y + index / width + 0.5f) / atlas.height);
                // Sample the original GPU texture in the glyph shader, rather than
                // converting its baked frame color into another multiplying tint.
            }
            catch (Exception error) { Debug.LogWarning("[Tweaks] Could not sample native HUD palette: " + error.Message); }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
                if (scratch != null) UnityEngine.Object.Destroy(scratch);
            }
        }
    }
}
