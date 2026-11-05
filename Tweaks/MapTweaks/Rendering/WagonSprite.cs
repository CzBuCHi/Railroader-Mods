using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks.MapTweaks.Rendering
{
    internal sealed class WagonSprite : IDisposable
    {
        private readonly Dictionary<Sprite, Sprite> generated = new Dictionary<Sprite, Sprite>();

        internal Sprite FromLocomotive(Sprite original)
        {
            if (generated.TryGetValue(original, out var existing) && existing != null) return existing;
            // The native sprite is not packed. Refuse an unexpected atlas layout
            // rather than mirroring the wrong region of a changed game asset.
            if (original.packed) throw new NotSupportedException("The locomotive map sprite is unexpectedly atlas-packed.");
            var rect = original.rect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            var sourceTexture = original.texture;
            var previous = RenderTexture.active;
            var target = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32);
            Texture2D? copy = null;
            try
            {
                // GPU readback also works when the game's texture is not CPU-readable.
                Graphics.Blit(sourceTexture, target);
                RenderTexture.active = target;
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(rect, 0, 0, false);
                var pixels = MirrorBottomHalf(copy.GetPixels32(), width, height);
                copy.SetPixels32(pixels);
                copy.wrapMode = TextureWrapMode.Clamp;
                copy.filterMode = sourceTexture.filterMode;
                copy.Apply(false, true);
                // Remove transparent padding so the requested wagon width describes its visible shape.
                var left = width; var right = -1; var bottom = height; var top = -1;
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a == 0) continue;
                        left = Math.Min(left, x); right = Math.Max(right, x);
                        bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                    }
                if (right < left) throw new InvalidOperationException("The locomotive map sprite is empty.");
                var bounds = new Rect(left, bottom, right - left + 1, top - bottom + 1);
                var sprite = Sprite.Create(copy, bounds, new Vector2(0.5f, 0.5f),
                    original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = "MapTweaks mirrored wagon";
                generated.Add(original, sprite);
                return sprite;
            }
            catch
            {
                if (copy != null) Object.Destroy(copy);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        // Unity pixel arrays start at the lower-left corner. Keep the lower half
        // and reflect it into the upper half, including its alpha and border pixels.
        private static T[] MirrorBottomHalf<T>(T[] pixels, int width, int height)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (width <= 0 || height <= 0 || (long)width * height != pixels.Length)
                throw new ArgumentException("Pixel dimensions must match the source array.");
            var result = new T[pixels.Length];
            for (var row = 0; row < height; row++)
                Array.Copy(pixels, Math.Min(row, height - 1 - row) * width, result, row * width, width);
            return result;
        }

        public void Dispose()
        {
            foreach (var sprite in generated.Values)
            {
                if (sprite == null) continue;
                var texture = sprite.texture;
                Object.Destroy(sprite);
                if (texture != null) Object.Destroy(texture);
            }
            generated.Clear();
        }
    }
}
