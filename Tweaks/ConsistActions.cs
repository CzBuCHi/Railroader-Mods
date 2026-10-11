using System;
using System.Linq;
using System.Reflection;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
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
internal static class ConsistActionsPatch
{
    [HarmonyPostfix]
    private static void Postfix(LocomotiveControlsUIAdapter __instance, TMP_Text ___speedLabel)
    {
        var actions = __instance.GetComponent<ConsistActions>() ?? __instance.gameObject.AddComponent<ConsistActions>();
        actions.Initialize(__instance, ___speedLabel);
    }
}

internal sealed class ConsistActions : MonoBehaviour
{
    private static readonly MethodInfo EndKey = AccessTools.Method(typeof(Car), "KeyValueKeyFor",
        new[] { typeof(Car.EndGearStateKey), typeof(Car.End) });
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
        if (!TweaksPlugin.Settings.ConsistActionButtons || !adapter.isActiveAndEnabled)
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
        var fixAir = cars.Any(car => NeedsAir(car, Car.LogicalEnd.A) || NeedsAir(car, Car.LogicalEnd.B));
        var brakes = cars.Count(car => car.air.handbrakeApplied);
        warningCar = FindWarningCar(cars);
        buttons[0].gameObject.SetActive(fixAir);
        buttons[1].gameObject.SetActive(brakes > 0);
        buttons[2].gameObject.SetActive(warningCar != null);
        buttons[2].GetComponent<ConsistIcon>().SetColor(warningCar != null && warningCar.HasHotbox
            ? new Color32(255, 65, 45, 255) : new Color32(255, 165, 45, 255));
        tooltips[1].TooltipInfo = new TooltipInfo("Release all handbrakes", "Release " + brakes + " applied handbrakes in this coupled consist.");
        if (warningCar != null)
            tooltips[2].TooltipInfo = new TooltipInfo(warningCar.HasHotbox ? "Hotbox" : "Low oil: " + Mathf.RoundToInt(warningCar.Oiled * 100f) + "%",
                warningCar.DisplayName + (warningCar.HasHotbox ? ": click to follow the car with a hotbox." : ": click to follow the car with the lowest bearing oil."));
        strip!.SetActive(fixAir || brakes > 0 || warningCar != null);
    }
    private static bool NeedsAir(Car car, Car.LogicalEnd end)
    {
        if (car.set == null) return false;
        var coupled = car.CoupledTo(end);
        return coupled != null ? car.AirConnectedTo(end) != coupled || car[end].AnglecockSetting < 0.999f
            : car[end].AnglecockSetting > 0.001f;
    }
    private void FixAir()
    {
        foreach (var car in CurrentCars())
        {
            foreach (var end in new[] { Car.LogicalEnd.A, Car.LogicalEnd.B })
            {
                if (car.set == null || !NeedsAir(car, end)) continue;
                var coupled = car.CoupledTo(end);
                var key = (string)EndKey.Invoke(null, new object[] { Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end) });
                StateManager.ApplyLocal(new PropertyChange(car.id, key, new FloatPropertyValue(coupled != null ? 1f : 0f)));
                if (coupled != null && car.AirConnectedTo(end) != coupled)
                    StateManager.ApplyLocal(new SetGladhandsConnected(car.id, coupled.id, true));
            }
        }
        nextScan = 0f;
    }
    private void ReleaseHandbrakes()
    {
        foreach (var car in CurrentCars())
            if (car.air.handbrakeApplied) car.SetHandbrake(false);
        nextScan = 0f;
    }
    private void FollowLowOil()
    {
        // Resolve the current consist at click time, not a stale selection.
        var car = FindWarningCar(CurrentCars());
        if (car != null) CameraSelector.shared.FollowCar(car);
    }
    private static Car? FindWarningCar(Car[] cars)
    {
        // A hotbox remains a fault even after oil has been refilled.
        return cars.Where(car => car.HasHotbox || (car.EnableOiling && car.Oiled < TweaksPlugin.Settings.OilWarningThreshold))
            .OrderByDescending(car => car.HasHotbox).ThenBy(car => car.Oiled).FirstOrDefault();
    }
    private bool CreateStrip()
    {
        var creator = Object.FindObjectOfType<ProgrammaticWindowCreator>();
        if (creator == null || creator.builderAssets == null) return false;
        if (EndKey == null) throw new NotSupportedException("Native anglecock property key method is unavailable.");
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
        Action[] clicks = { FixAir, ReleaseHandbrakes, FollowLowOil };
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
        tooltips[0].TooltipInfo = new TooltipInfo("Fix air", "Connect coupled gladhands, open anglecocks between coupled cars, and close anglecocks at the outer ends.");
        nextScan = 0f;
        return true;
    }
    private void OnDisable() { if (strip != null) strip.SetActive(false); }
    private void OnDestroy() { if (strip != null) Object.Destroy(strip); }
}