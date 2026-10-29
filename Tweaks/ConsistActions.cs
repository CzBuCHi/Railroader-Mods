﻿using System;
using System.Linq;
using Model;
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

    internal sealed class ConsistActions : MonoBehaviour
    {
        private LocomotiveControlsUIAdapter? adapter;
        private RectTransform? speedRect;
        private GameObject? strip;
        private RectTransform? stripRect;
        private readonly Button[] buttons = new Button[3];
        private readonly UITooltipProvider[] tooltips = new UITooltipProvider[3];
        private BaseLocomotive? lastLocomotive;
        private Car? warningCar;
        private float nextScan;
        private bool failed;

        internal void Initialize(LocomotiveControlsUIAdapter owner, TMP_Text speed)
        {
            adapter = owner; speedRect = speed.GetComponent<RectTransform>(); nextScan = 0f;
        }
        private Car[] CurrentCars()
        {
            var locomotive = TrainController.Shared?.SelectedLocomotive;
            return locomotive == null ? Array.Empty<Car>() : locomotive.EnumerateCoupled().Where(car => car != null).ToArray();
        }
        private void LateUpdate()
        {
            if (adapter == null || speedRect == null || failed) return;
            if (!adapter.isActiveAndEnabled)
            { if (strip != null) strip.SetActive(false); return; }
            if (strip == null)
            {
                try { if (!CreateStrip()) return; }
                catch (Exception error)
                {
                    failed = true;
                    if (strip != null) strip.SetActive(false);
                    Debug.LogError("[Tweaks] Could not create consist action buttons: " + error);
                    return;
                }
            }
            // Keep the icons beside the speed digits, above the consist indicator bar.
            stripRect!.position = speedRect.TransformPoint(new Vector3(speedRect.rect.xMin - 4f, speedRect.rect.yMax - 2f, 0f));
            var locomotive = TrainController.Shared?.SelectedLocomotive;
            if (lastLocomotive == locomotive && Time.unscaledTime < nextScan) return;
            lastLocomotive = locomotive; nextScan = Time.unscaledTime + 0.5f;
            var cars = CurrentCars();
            var airProblem = Settings.Current.ShowAirButton && cars.Any(car => NeedsAir(car, Car.LogicalEnd.A) || NeedsAir(car, Car.LogicalEnd.B));
            var brakes = Settings.Current.ShowHandbrakeButton ? cars.Count(car => car.air.handbrakeApplied) : 0;
            warningCar = Settings.Current.ShowOilButton ? FindWarningCar(cars) : null;
            buttons[0].gameObject.SetActive(airProblem);
            buttons[1].gameObject.SetActive(brakes > 0);
            buttons[2].gameObject.SetActive(warningCar != null);
            buttons[2].GetComponent<ConsistIcon>().SetColor(warningCar != null && warningCar.HasHotbox
                ? new Color32(255, 65, 45, 255) : new Color32(255, 165, 45, 255));
            tooltips[1].TooltipInfo = new TooltipInfo("Find applied handbrake", brakes + " applied handbrakes. Click to show a car needing attention; no brakes are changed.");
            if (warningCar != null)
                tooltips[2].TooltipInfo = new TooltipInfo(warningCar.HasHotbox ? "Hotbox" : "Low oil: " + Mathf.RoundToInt(warningCar.Oiled * 100f) + "%",
                    warningCar.DisplayName + (warningCar.HasHotbox ? ": click to show the car with a hotbox." : ": click to show the car with the lowest bearing oil."));
            strip!.SetActive(airProblem || brakes > 0 || warningCar != null);
        }
        private static bool NeedsAir(Car car, Car.LogicalEnd end)
        {
            if (car.set == null) return false;
            var coupled = car.CoupledTo(end);
            return HudRules.NeedsAir(car.set != null, coupled != null,
                coupled != null && car.AirConnectedTo(end) == coupled, car[end].AnglecockSetting);
        }
        private void ShowAirProblem()
        {
            var camera = CameraSelector.shared;
            if (camera == null) return;
            // Resolve again at click time; never write anglecocks or gladhands.
            foreach (var car in CurrentCars())
            foreach (var end in new[] { Car.LogicalEnd.A, Car.LogicalEnd.B })
            {
                if (!NeedsAir(car, end)) continue;
                MaintenanceCameraFocus.FollowAir(car, end);
                return;
            }
        }
        private void ShowAppliedHandbrake()
        {
            var car = CurrentCars().FirstOrDefault(candidate => candidate.air.handbrakeApplied);
            if (car != null && CameraSelector.shared != null) CameraSelector.shared.ZoomToCar(car);
        }
        private void ShowOilWarning()
        {
            // Resolve the current consist at click time, not a stale selection.
            var car = FindWarningCar(CurrentCars());
            if (car != null && CameraSelector.shared != null) MaintenanceCameraFocus.FollowOil(car);
        }
        private static Car? FindWarningCar(Car[] cars) =>
            HudRules.FindWarning(cars, car => car.EnableOiling, car => car.HasHotbox, car => car.Oiled);
        private bool CreateStrip()
        {
            var creator = WindowManager.Shared;
            if (creator == null || creator.builderAssets == null) return false;
            strip = new GameObject("CzBuCHi.Tweaks.ConsistActions", typeof(RectTransform));
            strip.SetActive(false);
            stripRect = strip.GetComponent<RectTransform>();
            stripRect.SetParent(speedRect!.parent, false);
            stripRect.anchorMin = stripRect.anchorMax = new Vector2(0f, 0f);
            stripRect.pivot = new Vector2(1f, 1f);
            stripRect.sizeDelta = new Vector2(76f, 22f);
            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleRight;
            var ignored = strip.AddComponent<LayoutElement>(); ignored.ignoreLayout = true;
            var icons = new[] { "AirValve", "HandbrakeWheel", "Flame" };
            Action[] clicks = { ShowAirProblem, ShowAppliedHandbrake, ShowOilWarning };
            for (var i = 0; i < buttons.Length; i++)
            {
                var result = creator.builderAssets.CreateButton(UIBuilderAssets.ButtonStyle.Compact, stripRect, clicks[i]);
                var rect = result.Item1;
                var fitter = rect.GetComponent<ContentSizeFitter>(); if (fitter != null) fitter.enabled = false;
                var buttonLayout = rect.GetComponent<LayoutGroup>(); if (buttonLayout != null) buttonLayout.enabled = false;
                var size = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = 24f; size.minHeight = size.preferredHeight = 22f;
                size.flexibleWidth = size.flexibleHeight = 0f;
                result.Item2.text = string.Empty; result.Item2.richText = true;
                result.Item2.enableAutoSizing = true; result.Item2.fontSizeMin = 9f; result.Item2.fontSizeMax = 16f;
                result.Item2.alignment = TextAlignmentOptions.Center; result.Item2.margin = Vector4.zero;
                var labelRect = result.Item2.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.one; labelRect.offsetMax = -Vector2.one;
                rect.gameObject.AddComponent<ConsistIcon>().Initialize(result.Item2, icons[i],
                    i == 2 ? new Color32(255, 165, 45, 255) : Color.white);
                buttons[i] = rect.GetComponent<Button>();
                tooltips[i] = rect.GetComponent<UITooltipProvider>() ?? rect.gameObject.AddComponent<UITooltipProvider>();
            }
            tooltips[0].TooltipInfo = new TooltipInfo("Find air problem", "Follow the affected coupler end while the car moves. Hoses and valves are not changed.");
            nextScan = 0f;
            return true;
        }
        private void OnDisable() { if (strip != null) strip.SetActive(false); }
        private void OnDestroy() { if (strip != null) Object.Destroy(strip); }
    }
}
