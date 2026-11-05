using System;
using HarmonyLib;
using Model;
using TMPro;
using UI.Map;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks.MapTweaks.Rendering
{
    internal sealed class VehicleMarker
    {
        private static readonly System.Reflection.FieldInfo Prefabs = AccessTools.Field(typeof(Car), "_setupPrefabs");
        internal readonly GameObject Root;
        internal readonly Canvas Canvas;
        internal readonly Image Body;
        private readonly MapIcon? locomotiveIcon;
        private readonly Vector3 locomotiveScale;
        private readonly float wagonWidth;
        private string? number;

        private VehicleMarker(GameObject root, Canvas canvas, Image body, MapIcon? locomotiveIcon, float wagonWidth = 0)
        {
            Root = root; Canvas = canvas; Body = body;
            this.locomotiveIcon = locomotiveIcon;
            locomotiveScale = body.transform.localScale;
            this.wagonWidth = wagonWidth;
        }

        internal static VehicleMarker Create(Car car, Transform parent, UnityEngine.Camera camera, WagonSprite wagonSprite)
        {
            var template = ((Car.SetupPrefabs)Prefabs.GetValue(car)).LocomotiveMapIcon;
            if (template == null) throw new InvalidOperationException("The game map icon prefab is unavailable.");
            if (!(car is BaseLocomotive))
            {
                // Wagons use a runtime-generated symmetric variant of the game sprite, without text.
                var nativeImage = template.GetComponentInChildren<Image>(true)
                    ?? throw new InvalidOperationException("The locomotive map image is unavailable.");
                var original = nativeImage.sprite
                    ?? throw new InvalidOperationException("The locomotive map sprite is unavailable.");
                var sprite = wagonSprite.FromLocomotive(original);
                // Measure the native image in icon-root units, including all child scales.
                // Its transparent margins are absent from the generated sprite.
                var imageWidth = nativeImage.rectTransform.rect.width;
                if (nativeImage.preserveAspect)
                    imageWidth = Mathf.Min(imageWidth, nativeImage.rectTransform.rect.height * original.rect.width / original.rect.height);
                var nativeWidth = template.transform.InverseTransformVector(
                    nativeImage.transform.TransformVector(Vector3.right * imageWidth)).magnitude;
                var matchingWidth = VehicleMarkerGeometry.Width(nativeWidth, 1f, sprite.rect.width / original.rect.width);
                var root = new GameObject("Wagon marker", typeof(RectTransform), typeof(Canvas));
                root.SetActive(false);
                root.layer = template.gameObject.layer;
                root.transform.SetParent(parent, false);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                var shape = new GameObject("Wagon shape", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shape.layer = root.layer;
                shape.transform.SetParent(root.transform, false);
                var image = shape.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                root.SetActive(true);
                return new VehicleMarker(root, canvas, image, null, matchingWidth);
            }

            // Locomotive graphics and caption rotation are provided by Railroader itself.
            var icon = Object.Instantiate(template, parent, false);
            var body = icon.GetComponentInChildren<Image>(true);
            var caption = icon.GetComponentInChildren<TMP_Text>(true);
            if (body == null || caption == null)
            {
                Object.Destroy(icon.gameObject);
                throw new InvalidOperationException("The game locomotive icon layout has changed.");
            }
            icon.name = "Locomotive marker";
            caption.richText = false;
            caption.color = Color.black;
            caption.overflowMode = TextOverflowModes.Overflow;
            caption.maxVisibleCharacters = caption.maxVisibleLines = caption.maxVisibleWords = int.MaxValue;
            caption.firstVisibleCharacter = 0;
            caption.maskable = false;
            foreach (var collider in icon.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var graphic in icon.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            icon.OnClick = null;
            return new VehicleMarker(icon.gameObject, icon.GetComponent<Canvas>(), body, icon);
        }

        internal void UpdateAppearance(string label, Color color, float length, float engineLength, float zoom, float metersPerPixel)
        {
            Body.color = color;
            if (locomotiveIcon == null)
            {
                Body.rectTransform.sizeDelta = new Vector2(VehicleMarkerGeometry.Width(wagonWidth, zoom, 1f),
                    VehicleMarkerGeometry.Length(length, metersPerPixel));
                return;
            }
            var scale = locomotiveScale;
            scale.y *= length / Mathf.Max(0.01f, engineLength);
            Body.transform.localScale = scale;
            if (number != label) { locomotiveIcon.SetText(label); number = label; }
            locomotiveIcon.SetZoom(zoom);
        }

        internal void Destroy()
        {
            if (Root == null) return;
            Root.SetActive(false);
            Object.Destroy(Root);
        }
    }
}
