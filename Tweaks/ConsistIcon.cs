using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tweaks;

// Each button owns its monochrome copy. Native atlases remain unchanged.
internal sealed class ConsistIcon : MonoBehaviour
{
    private Texture2D? texture;
    private Sprite? sprite;
    private Image? iconImage;
    private TMP_Text? fallbackLabel;
    internal void SetColor(Color color)
    {
        if (iconImage != null) iconImage.color = color;
        if (fallbackLabel != null) fallbackLabel.color = color;
    }
    internal void Initialize(TMP_Text label, string name, Color color)
    {
        try { texture = name == "AirValve" ? AirValve() : NativeMask(label, name); }
        catch (Exception error)
        {
            // Keep every action available even if a native atlas changes.
            Debug.LogWarning("[Tweaks] Using text fallback for icon " + name + ": " + error.Message);
            label.text = name == "HandbrakeWheel" ? "HB" : name == "Flame" ? "!" : "AIR";
            label.color = color; fallbackLabel = label;
            return;
        }
        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rect = icon.GetComponent<RectTransform>(); rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 2f); rect.offsetMax = new Vector2(-3f, -2f);
        var image = icon.GetComponent<Image>(); iconImage = image; image.sprite = sprite; image.color = color;
        image.preserveAspect = true; image.raycastTarget = false;
        // RailroaderButton.Awake and DoStateTransition require an active TMP child.
        label.text = string.Empty;
    }
    private static Texture2D NativeMask(TMP_Text label, string name)
    {
        var root = label.spriteAsset != null ? label.spriteAsset : TMP_Settings.defaultSpriteAsset;
        var asset = TMP_SpriteAsset.SearchForSpriteByHashCode(root, TMP_TextUtilities.GetHashCode(name), true, out var index);
        if (asset == null || index < 0) throw new NotSupportedException("Missing native icon " + name);
        var glyph = asset.spriteCharacterTable[index].glyph.glyphRect;
        var atlas = asset.spriteSheet;
        var temp = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        Texture2D? result = null;
        try
        {
            Graphics.Blit(atlas, temp); RenderTexture.active = temp;
            result = new Texture2D(glyph.width, glyph.height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(glyph.x, glyph.y, glyph.width, glyph.height), 0, 0);
            var pixels = result.GetPixels32();
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, pixels[i].a);
            result.SetPixels32(pixels); result.Apply(false, true);
            result.filterMode = FilterMode.Bilinear; result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }
        catch { if (result != null) Object.Destroy(result); throw; }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temp); }
    }
    private static Texture2D AirValve()
    {
        // Hose with a T-handle anglecock, drawn as a small antialiased UI glyph.
        const int size = 64;
        var pixels = new Color32[size * size];
        Vector2[] points = { new Vector2(8, 19), new Vector2(31, 19), new Vector2(31, 43), new Vector2(54, 43) };
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f); var distance = float.MaxValue;
            for (var i = 0; i < points.Length - 1; i++) distance = Mathf.Min(distance, Segment(p, points[i], points[i + 1]));
            distance = Mathf.Min(distance, Segment(p, new Vector2(31, 30), new Vector2(46, 30)));
            distance = Mathf.Min(distance, Segment(p, new Vector2(46, 23), new Vector2(46, 37)));
            distance = Mathf.Min(distance, Segment(p, new Vector2(8, 13), new Vector2(8, 25)));
            distance = Mathf.Min(distance, Segment(p, new Vector2(54, 37), new Vector2(54, 49)));
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(2.8f - distance)));
        }
        var result = new Texture2D(size, size, TextureFormat.RGBA32, false);
        result.SetPixels32(pixels); result.Apply(false, true); result.filterMode = FilterMode.Bilinear;
        result.wrapMode = TextureWrapMode.Clamp; return result;
    }
    private static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var line = b - a; return Vector2.Distance(p, a + line * Mathf.Clamp01(Vector2.Dot(p - a, line) / line.sqrMagnitude));
    }
    private void OnDestroy()
    {
        if (sprite != null) Object.Destroy(sprite);
        if (texture != null) Object.Destroy(texture);
    }
}
