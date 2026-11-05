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

namespace Tweaks.Hud
{

    internal sealed class ControlModeButtons : MonoBehaviour
    {
        private LocomotiveControlsUIAdapter? adapter;
        private TMP_Dropdown? dropdown;
        private GameObject? strip;
        private UIBuilderAssets? assets;
        private readonly Button[] buttons = new Button[3];
        private readonly ControlModeIcon[] icons = new ControlModeIcon[3];
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
            for (var i = 0; i < buttons.Length; i++)
            {
                var active = i == highlighted;
                icons[i].SetSelected(active);
            }
        }

        private bool CreateStrip()
        {
            var creator = WindowManager.Shared;
            if (creator == null || creator.builderAssets == null) return false;
            assets = creator.builderAssets;
            var source = dropdown!.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases(); // Measure native spacing after the 75%/other UI scale layout has settled.
            var style = HudButtonStyle.Find(source, assets.buttonCompact);
            var coordinates = (RectTransform)source.parent;
            var sourceBounds = HudButtonStyle.Bounds(source, coordinates);
            var candidate = new GameObject("CzBuCHi.Tweaks.ControlModes", typeof(RectTransform));
            candidate.SetActive(false);
            try
            {
            var rect = candidate.GetComponent<RectTransform>();
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot; rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = source.anchoredPosition;
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            var width = style.Width * buttons.Length + style.Gap * (buttons.Length - 1);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, style.Height);
            var ownBounds = HudButtonStyle.Bounds(rect, coordinates);
            var left = style.RowLeft.HasValue && Mathf.Abs(style.RowLeft.Value - sourceBounds.xMin) < style.Width * 2 ? style.RowLeft.Value : sourceBounds.xMin;
            rect.anchoredPosition += new Vector2(left - ownBounds.xMin, sourceBounds.center.y - ownBounds.center.y);
            rect.SetSiblingIndex(source.GetSiblingIndex());
            var sourceLayout = source.GetComponent<LayoutElement>();
            if (sourceLayout != null)
            {
                var layout = candidate.AddComponent<LayoutElement>();
                layout.ignoreLayout = sourceLayout.ignoreLayout;
                layout.minWidth = layout.preferredWidth = width; layout.minHeight = layout.preferredHeight = style.Height;
                layout.flexibleWidth = layout.flexibleHeight = 0;
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
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0, 0.5f);
                buttonRect.pivot = new Vector2(0, 0.5f);
                buttonRect.sizeDelta = new Vector2(style.Width, style.Height);
                buttonRect.anchoredPosition = new Vector2(i * (style.Width + style.Gap), 0);
                style.Apply(buttonRect.GetComponent<Button>());
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
                icons[i] = glyph.AddComponent<ControlModeIcon>();
                icons[i].Initialize(buttonRect.GetComponent<Button>(), style, i);
                var tooltip = buttonRect.GetComponent<UITooltipProvider>() ?? buttonRect.gameObject.AddComponent<UITooltipProvider>();
                tooltip.TooltipInfo = new TooltipInfo(Titles[i], Descriptions[i]);
                buttons[i] = buttonRect.GetComponent<Button>();
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
