using System.Collections.Generic;
using Helpers;
using Track;
using UI.Builder;
using UI.Map;
using UI.Windows;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
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
            root = new GameObject("CzBuCHi.WaybillMap.Controls", typeof(RectTransform), typeof(Image));
            var rect = root.GetComponent<RectTransform>(); rect.SetParent(window.contentRectTransform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-10, -10); rect.sizeDelta = new Vector2(255, 130);
            root.GetComponent<Image>().color = new Color(0.1f, 0.095f, 0.08f, 0.95f);
            panel = UIPanel.Create(rect, UIBuilderAssets.Shared, Build);
            root.SetActive(window.IsShown);
        }

        private void Build(UIPanelBuilder builder)
        {
            builder.ConfigureLayout(4f, new RectOffset(8, 8, 8, 8), TextAnchor.UpperLeft);
            builder.HStack(row =>
            {
                row.ConfigureLayout(6f);
                row.AddButtonCompact("Save", () => WaybillMapPlugin.Active!.Switches.Save()).Width(112).Height(24)
                    .Tooltip("Save switches", "Save the IDs of reversed switches. Normal positions are implicit.");
                row.AddButtonCompact("Restore", () => WaybillMapPlugin.Active!.Switches.Restore()).Width(112).Height(24)
                    .Tooltip("Restore switches", "Restore saved positions, respecting blocked switches, CTC locks and permissions.");
            }).Height(24);
            builder.HStack(row =>
            {
                row.ConfigureLayout(5f);
                row.AddToggle(() => WaybillMapPlugin.FollowOption.IsOn, value => WaybillMapPlugin.FollowOption.IsOn = value).Width(20).Height(20);
                row.AddLabel("Follow selected car").Height(22);
            }).Height(22);
            builder.AddDropdown(new List<string> { "Locomotives only", "Locomotives + caboose", "All vehicles" },
                WaybillMapPlugin.VehicleMode, value => WaybillMapPlugin.VehicleMode = value).Height(26);
            builder.AddLabel(() => WaybillMapPlugin.Active?.Switches.Status ?? "", UIPanelBuilder.Frequency.Periodic).Height(22);
        }

        private void LateUpdate()
        {
            if (root == null || window == null) return;
            root.SetActive(window.IsShown); root.transform.SetAsLastSibling();
            if (!window.IsShown || map == null || !WaybillMapPlugin.FollowOption.IsOn || Graph.Shared == null) return;
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
}
