﻿using HarmonyLib;
using TMPro;
using UI;
using UI.Builder;
using UI.EngineControls;
using UI.Tooltips;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tweaks
{

    internal sealed class ControlModeButtons : MonoBehaviour
    {
        private LocomotiveControlsUIAdapter? adapter;
        private TMP_Dropdown? dropdown;
        private GameObject? strip;
        private UIBuilderAssets? assets;
        private readonly Button[] buttons = new Button[3];
        private readonly Graphic[] backgrounds = new Graphic[3];
        private readonly Sprite?[] normalSprites = new Sprite?[3];
        private readonly Color[] normalColors = new Color[3];
        private readonly ColorBlock[] normalButtonColors = new ColorBlock[3];
        private int highlighted = -1;
        private bool creationFailed;
        private static readonly string[] Titles = { "Manual", "AE Road", "AE Yard" };
        private static readonly string[] Descriptions = {
            "Control the locomotive manually.", "Auto Engineer: road mode.",
            "Auto Engineer: yard mode."
        };

        internal void Initialize(LocomotiveControlsUIAdapter owner, TMP_Dropdown nativeDropdown)
        {
            adapter = owner; dropdown = nativeDropdown;
        }

        private void LateUpdate()
        {
            if (adapter == null || dropdown == null) return;
            if (!adapter.isActiveAndEnabled)
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
            var selectedMode = HudRules.HighlightedMode(dropdown.value);
            if (highlighted == selectedMode) return;
            highlighted = selectedMode;
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
            var creator = WindowManager.Shared;
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
                buttonRect.anchorMin = new Vector2(i / (float)buttons.Length, 0f);
                buttonRect.anchorMax = new Vector2((i + 1) / (float)buttons.Length, 1f);
                buttonRect.offsetMin = new Vector2(i == 0 ? 0f : 1f, 0f);
                buttonRect.offsetMax = new Vector2(i == buttons.Length - 1 ? 0f : -1f, 0f);
                var label = result.Item2;
                label.text = string.Empty;
                label.enableAutoSizing = true; label.fontSizeMin = 9f; label.fontSizeMax = 16f;
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(1f, 1f); labelRect.offsetMax = new Vector2(-1f, -1f);
                var glyph = new GameObject("ModeIcon", typeof(RectTransform));
                var glyphRect = glyph.GetComponent<RectTransform>();
                glyphRect.SetParent(buttonRect, false);
                glyphRect.anchorMin = Vector2.zero; glyphRect.anchorMax = Vector2.one;
                glyphRect.offsetMin = new Vector2(3f, 2f); glyphRect.offsetMax = new Vector2(-3f, -2f);
                glyph.AddComponent<ControlModeIcon>().Initialize(label, i);
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
}
