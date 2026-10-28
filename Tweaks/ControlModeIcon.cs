using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tweaks
{
    // Vector glyphs stay sharp at different UI scales and require no sprite atlas.
    internal sealed class ControlModeIcon : MaskableGraphic
    {
        private Button? button;
        private Color hudColor;
        private Texture? paletteTexture;
        private Vector2 paletteUV;
        private bool selected;
        private int mode;

        public override Texture mainTexture => !selected && paletteTexture != null ? paletteTexture : Texture2D.whiteTexture;

        internal void Initialize(Button owner, HudButtonStyle style, int index)
        {
            button = owner; hudColor = style.GlyphTint; mode = index; raycastTarget = false;
            paletteTexture = style.PaletteTexture; paletteUV = style.PaletteUV; material = style.Material;
            RefreshColor(); SetVerticesDirty();
        }

        internal void SetSelected(bool value)
        { if (selected == value) return; selected = value; RefreshColor(); SetMaterialDirty(); SetVerticesDirty(); }

        private void LateUpdate()
        {
            RefreshColor();
        }

        private void RefreshColor()
        {
            // Dialog label colors differ from the surrounding locomotive HUD icons.
            Color tint = selected ? Color.white : hudColor;
            if (button != null && !button.interactable) tint.a *= 0.5f;
            if (color != tint) color = tint;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (mode == 0)
            {
                // Open hand: thumb, four fingers and palm.
                Path(mesh, 3.2f, 22,10, 15,20, 8,33, 8,38, 12,40, 20,31,
                    20,52, 24,55, 28,52, 28,35, 28,58, 32,60, 36,57, 36,35,
                    36,55, 40,57, 44,54, 44,34, 44,47, 48,49, 52,46, 52,24,
                    48,14, 43,10, 22,10);
            }
            else if (mode == 1)
            {
                // Main line: parallel rails and sleepers.
                for (var y = 12; y <= 54; y += 7) Path(mesh, 2.5f, 14,y, 50,y);
                Path(mesh, 3.5f, 21,8, 21,58);
                Path(mesh, 3.5f, 43,8, 43,58);
            }
            else
            {
                // Yard: a straight track with a turnout diverging to the right.
                for (var y = 12; y <= 54; y += 7) Path(mesh, 2.5f, 8,y, 34,y);
                Path(mesh, 3.5f, 14,8, 14,58);
                Path(mesh, 3.5f, 28,8, 28,58);
                Path(mesh, 3.5f, 14,23, 22,36, 43,58);
                Path(mesh, 3.5f, 28,23, 36,36, 57,58);
                Path(mesh, 2.5f, 26,43, 44,31);
                Path(mesh, 2.5f, 33,50, 51,38);
                Path(mesh, 2.5f, 40,57, 58,45);
            }
        }

        private void Path(VertexHelper mesh, float width, params float[] points)
        {
            var rect = GetPixelAdjustedRect();
            var scale = Math.Min(rect.width, rect.height) / 64f;
            var origin = rect.center - Vector2.one * (32f * scale);
            for (var i = 0; i + 3 < points.Length; i += 2)
            {
                var a = origin + new Vector2(points[i], points[i + 1]) * scale;
                var b = origin + new Vector2(points[i + 2], points[i + 3]) * scale;
                var delta = (b - a).normalized;
                var normal = new Vector2(-delta.y, delta.x) * (width * scale / 2f);
                var start = mesh.currentVertCount;
                var uv = !selected && paletteTexture != null ? paletteUV : Vector2.zero;
                mesh.AddVert(a - normal, color, uv);
                mesh.AddVert(a + normal, color, uv);
                mesh.AddVert(b + normal, color, uv);
                mesh.AddVert(b - normal, color, uv);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
