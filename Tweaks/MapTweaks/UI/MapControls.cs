using System.Collections.Generic;
using HarmonyLib;
using Helpers;
using TMPro;
using Track;
using Tweaks.MapTweaks.Configuration;
using Tweaks.MapTweaks;
using UI.Builder;
using UI.Map;
using UI.Tooltips;
using UI.Windows;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks.MapTweaks.UI
{
    [DefaultExecutionOrder(1050)]
    internal sealed class MapControls : MonoBehaviour
    {
        private Window? window;
        private MapBuilder? map;
        private GameObject? root;
        private UIPanel? panel;

        internal void Initialize(MapBuilder builder)
        {
            window = GetComponent<Window>(); map = builder;
            if (root != null || window == null) return;
            root = new GameObject("CzBuCHi.MapTweaks.Controls", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>(); rect.SetParent(window.contentRectTransform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-10, -10); rect.sizeDelta = new Vector2(300, 84);
            root.GetComponent<Image>().color = new Color(0.1f, 0.095f, 0.08f, 0.95f);
            panel = UIPanel.Create(rect, UIBuilderAssets.Shared, Build);
            root.SetActive(window.IsShown);
        }

        private void Build(UIPanelBuilder builder)
        {
            builder.ConfigureLayout(4f, new RectOffset(6, 6, 4, 4), TextAnchor.UpperLeft);
            builder.HStack(row =>
            {
                row.ConfigureLayout(4f);
                var heading = row.AddLabel("Switch positions", text => text.fontSize = 14).Width(100).Height(22);
                var tooltip = heading.GetComponent<UITooltipProvider>() ?? heading.gameObject.AddComponent<UITooltipProvider>();
                tooltip.DynamicTooltipInfo = () => new TooltipInfo("Switch positions", MapTweaksFeature.Active?.Switches.Status ?? "");
                row.AddButtonCompact("Save", () => MapTweaksFeature.Active!.Switches.Save()).Width(84).Height(22)
                    .Tooltip("Save switches", "Save the IDs of reversed switches. Normal positions are implicit.");
                row.AddButtonCompact("Restore", () => MapTweaksFeature.Active!.Switches.Restore()).Width(84).Height(22)
                    .Tooltip("Restore switches", "Restore saved positions, respecting blocked switches, CTC locks and permissions.");
            }).Height(22);
            builder.HStack(row =>
            {
                row.ConfigureLayout(4f);
                row.AddLabel("Render", text => { text.fontSize = 12; text.alignment = TextAlignmentOptions.MidlineRight; text.margin = new Vector4(0, 0, 20, 0); }).Width(100).Height(22);
                row.AddDropdown(new List<string> { "Locomotives only", "Locomotives and cabooses", "All vehicles" },
                    (int)MapTweaksFeature.VehicleDisplayMode, value => MapTweaksFeature.VehicleDisplayMode = (VehicleDisplayMode)value).Width(172).Height(22);
            }).Height(22);
            builder.HStack(row =>
            {
                row.ConfigureLayout(4f);
                row.Spacer(100);
                row.AddToggle(() => MapTweaksFeature.FollowSelectedCar, value => MapTweaksFeature.FollowSelectedCar = value).Width(16).Height(16);
                row.AddLabel("Follow selected car", text => text.fontSize = 14).Height(22);
            }).Height(22);
        }

        private void LateUpdate()
        {
            if (root == null || window == null) return;
            root.SetActive(window.IsShown); root.transform.SetAsLastSibling();
            if (!window.IsShown || map == null || !MapTweaksFeature.FollowSelectedCar || Graph.Shared == null) return;
            var car = TrainController.Shared?.SelectedCar;
            if (car != null && car.LocationF.IsValid && car.LocationR.IsValid) map.SetMapCenter(car.GetCenterPosition());
        }

        internal void Shutdown()
        {
            enabled = false;
            if (root != null) root.SetActive(false);
            panel?.Dispose(); panel = null;
            if (root != null) { Object.Destroy(root); root = null; }
        }
        private void OnDestroy() => Shutdown();
    }

    [HarmonyPatch(typeof(MapDrag), nameof(MapDrag.OnDrag))]
    internal static class StopFollowingOnMapDrag
    {
        [HarmonyPrepare]
        private static bool Prepare() => global::Tweaks.Configuration.Settings.Current.MapTweaks.Enabled;

        [HarmonyPrefix]
        private static void Prefix(MapDrag __instance, PointerEventData data)
        {
            // Stop before native OnDragChange applies the pan, so LateUpdate cannot undo it.
            if (MapTweaksFeature.Active != null && __instance.GetComponentInParent<MapControls>() != null && data.delta.sqrMagnitude > 0 && MapTweaksFeature.FollowSelectedCar)
            {
                MapTweaksFeature.FollowSelectedCar = false;
                __instance.OnDragStart?.Invoke(); // Rebase after any movement since mouse-down while following.
            }
        }
    }
}
