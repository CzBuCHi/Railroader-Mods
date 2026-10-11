using System;
using System.IO;
using System.Linq;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwitchNormalizer;

internal sealed class ToolbarButton : MonoBehaviour
{
    private const string ButtonName = "CzBuCHi.SwitchNormalizer.Button";
    private UnityAction? onClick;
    private GameObject? buttonObject;
    private Sprite? icon;
    private Texture2D? texture;
    private bool mapLoaded;
    private float nextAttempt;
    private bool reportedError;

    internal void Initialize(UnityAction click)
    {
        onClick = click;
    }

    internal void SetMapLoaded(bool loaded)
    {
        mapLoaded = loaded;
        nextAttempt = 0f;
        if (!loaded)
            ClearButton();
    }

    private void Update()
    {
        if (!mapLoaded || buttonObject != null || Time.unscaledTime < nextAttempt)
            return;

        nextAttempt = Time.unscaledTime + 0.5f;
        try
        {
            // MapDidLoad can precede toolbar creation. Retry until the UI exists.
            var area = Object.FindObjectOfType<global::UI.TopRightArea>();
            if (area == null)
                return;
            var strip = area.transform.Find("Strip");
            if (strip == null)
                return;
            var template = strip.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name != ButtonName && button.GetComponent<Image>() != null);
            if (template == null)
                return;

            EnsureIcon();
            buttonObject = Object.Instantiate(template.gameObject, template.transform.parent);
            buttonObject.SetActive(false);
            buttonObject.name = ButtonName;
            buttonObject.transform.SetSiblingIndex(9);

            var button = buttonObject.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(onClick!);
            button.interactable = true;
            var tooltip = buttonObject.GetComponent<UITooltipProvider>()
                ?? buttonObject.AddComponent<UITooltipProvider>();
            tooltip.TooltipInfo = new TooltipInfo("Switch Normalizer", "Click: restore switches. Shift + click: save current positions.");
            var image = buttonObject.GetComponent<Image>();
            image.sprite = icon;
            image.overrideSprite = null;
            buttonObject.SetActive(true);
            reportedError = false;
        }
        catch (Exception error)
        {
            ClearButton();
            if (!reportedError)
                Debug.LogError("[SwitchNormalizer] Could not create toolbar button: " + error);
            reportedError = true;
            nextAttempt = Time.unscaledTime + 5f;
        }
    }

    private void EnsureIcon()
    {
        if (icon != null)
            return;

        using var stream = typeof(ToolbarButton).Assembly.GetManifestResourceStream("SwitchNormalizer.icon.png");
        if (stream == null)
            throw new InvalidOperationException("Embedded toolbar icon is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        if (texture != null)
            Object.Destroy(texture);
        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes.ToArray()))
            throw new InvalidOperationException("Embedded toolbar icon could not be decoded.");
        // Native toolbar tint supplies the visible color. Keep only the icon alpha
        // so baked-in gray is not multiplied by the same gray a second time.
        var pixels = texture.GetPixels32();
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i].r = 255;
            pixels[i].g = 255;
            pixels[i].b = 255;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    internal void ClearButton()
    {
        if (buttonObject == null)
            return;
        buttonObject.SetActive(false);
        buttonObject.GetComponent<Button>().onClick.RemoveAllListeners();
        Object.Destroy(buttonObject);
        buttonObject = null;
    }

    private void OnDestroy()
    {
        ClearButton();
        if (icon != null)
            Object.Destroy(icon);
        if (texture != null)
            Object.Destroy(texture);
    }
}
