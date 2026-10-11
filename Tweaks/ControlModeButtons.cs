using HarmonyLib;
using TMPro;
using UI;
using UI.Builder;
using UI.EngineControls;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tweaks;

[HarmonyPatch(typeof(LocomotiveControlsUIAdapter), "OnEnable")]
internal static class ControlModeButtonsPatch
{
    [HarmonyPostfix]
    private static void Postfix(LocomotiveControlsUIAdapter __instance, TMP_Dropdown ___modeDropdown)
    {
        var buttons = __instance.GetComponent<ControlModeButtons>() ?? __instance.gameObject.AddComponent<ControlModeButtons>();
        buttons.Initialize(__instance, ___modeDropdown);
    }
}

internal sealed class ControlModeButtons : MonoBehaviour
{
    private LocomotiveControlsUIAdapter? adapter;
    private TMP_Dropdown? dropdown;
    private GameObject? strip;
    private UIBuilderAssets? assets;
    private readonly Button[] buttons = new Button[4];
    private readonly Graphic[] backgrounds = new Graphic[4];
    private readonly Sprite?[] normalSprites = new Sprite?[4];
    private readonly Color[] normalColors = new Color[4];
    private readonly ColorBlock[] normalButtonColors = new ColorBlock[4];
    private int highlighted = -1;
    private bool creationFailed;
    private static readonly string[] Titles = { "Manual", "AE Road", "AE Yard", "AE Waypoint" };
    private static readonly string[] Icons = { TextSprites.MouseLeft, TextSprites.Coupled, TextSprites.Spotted, TextSprites.Destination };
    private static readonly string[] Descriptions = {
        "Control the locomotive manually.", "Auto Engineer: road mode.",
        "Auto Engineer: yard mode.", "Auto Engineer: waypoint mode."
    };

    internal void Initialize(LocomotiveControlsUIAdapter owner, TMP_Dropdown nativeDropdown)
    {
        adapter = owner; dropdown = nativeDropdown;
    }

    private void LateUpdate()
    {
        if (adapter == null || dropdown == null) return;
        if (!TweaksPlugin.Settings.ControlModeButtons || !adapter.isActiveAndEnabled)
        { RestoreDropdown(); return; }
        if (strip == null)
        {
            if (creationFailed) return;
            try { if (!CreateStrip()) return; }
            catch (System.Exception error)
            {
                creationFailed = true;
                RestoreDropdown();
                Debug.LogError("[Tweaks] Could not create control mode buttons; retained native dropdown: " + error);
                return;
            }
        }
        // TMP_Dropdown.Hide() calls Select(), even when already closed.
        // OnDisable cleans up the list without stealing keyboard focus.
        if (dropdown.gameObject.activeSelf) dropdown.gameObject.SetActive(false);
        strip!.SetActive(true);
        for (var i = 0; i < buttons.Length; i++) buttons[i].interactable = dropdown.interactable;
        if (highlighted == dropdown.value) return;
        highlighted = dropdown.value;
        var selected = assets!.buttonSelected;
        var selectedBackground = selected.targetGraphic;
        for (var i = 0; i < buttons.Length; i++)
        {
            var active = i == highlighted;
            buttons[i].colors = active ? selected.colors : normalButtonColors[i];
            if (backgrounds[i] is Image image)
                image.sprite = active && selectedBackground is Image selectedImage ? selectedImage.sprite : normalSprites[i];
            backgrounds[i].color = active && selectedBackground != null ? selectedBackground.color : normalColors[i];
        }
    }

    private bool CreateStrip()
    {
        var creator = Object.FindObjectOfType<ProgrammaticWindowCreator>();
        if (creator == null || creator.builderAssets == null) return false;
        assets = creator.builderAssets;
        var source = dropdown!.GetComponent<RectTransform>();
        var candidate = new GameObject("CzBuCHi.Tweaks.ControlModes", typeof(RectTransform));
        candidate.SetActive(false);
        try
        {
        var rect = candidate.GetComponent<RectTransform>();
        rect.SetParent(source.parent, false);
        rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot; rect.sizeDelta = source.sizeDelta;
        rect.anchoredPosition = source.anchoredPosition;
        rect.localRotation = source.localRotation; rect.localScale = source.localScale;
        rect.SetSiblingIndex(source.GetSiblingIndex());
        var sourceLayout = source.GetComponent<LayoutElement>();
        if (sourceLayout != null)
        {
            var layout = candidate.AddComponent<LayoutElement>();
            layout.ignoreLayout = sourceLayout.ignoreLayout;
            layout.minWidth = sourceLayout.minWidth; layout.minHeight = sourceLayout.minHeight;
            layout.preferredWidth = sourceLayout.preferredWidth; layout.preferredHeight = sourceLayout.preferredHeight;
            layout.flexibleWidth = sourceLayout.flexibleWidth; layout.flexibleHeight = sourceLayout.flexibleHeight;
        }
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var result = assets.CreateButton(UIBuilderAssets.ButtonStyle.Compact, rect, () => {
                if (adapter != null && dropdown != null && dropdown.interactable)
                    adapter.DropdownDidChange(index);
            });
            var buttonRect = result.Item1;
            var fitter = buttonRect.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            var buttonLayout = buttonRect.GetComponent<LayoutGroup>();
            if (buttonLayout != null) buttonLayout.enabled = false;
            buttonRect.anchorMin = new Vector2(i / 4f, 0f);
            buttonRect.anchorMax = new Vector2((i + 1) / 4f, 1f);
            buttonRect.offsetMin = new Vector2(i == 0 ? 0f : 1f, 0f);
            buttonRect.offsetMax = new Vector2(i == 3 ? 0f : -1f, 0f);
            var label = result.Item2;
            label.text = Icons[i].Replace("<sprite ", "<sprite tint=1 "); label.richText = true;
            label.enableAutoSizing = true; label.fontSizeMin = 9f; label.fontSizeMax = 16f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(1f, 1f); labelRect.offsetMax = new Vector2(-1f, -1f);
            var tooltip = buttonRect.GetComponent<UITooltipProvider>() ?? buttonRect.gameObject.AddComponent<UITooltipProvider>();
            tooltip.TooltipInfo = new TooltipInfo(Titles[i], Descriptions[i]);
            buttons[i] = buttonRect.GetComponent<Button>();
            backgrounds[i] = buttons[i].targetGraphic ??
                throw new System.InvalidOperationException("Native compact button has no target graphic.");
            normalSprites[i] = (backgrounds[i] as Image)?.sprite;
            normalColors[i] = backgrounds[i].color;
            normalButtonColors[i] = buttons[i].colors;
        }
        highlighted = -1;
        strip = candidate;
        return true;
        }
        catch
        {
            Object.Destroy(candidate);
            throw;
        }
    }

    private void RestoreDropdown()
    {
        if (strip != null) strip.SetActive(false);
        if (dropdown != null) dropdown.gameObject.SetActive(true);
        highlighted = -1;
    }
    private void OnDisable() => RestoreDropdown();
    private void OnDestroy()
    {
        RestoreDropdown();
        if (strip != null) Object.Destroy(strip);
    }
}