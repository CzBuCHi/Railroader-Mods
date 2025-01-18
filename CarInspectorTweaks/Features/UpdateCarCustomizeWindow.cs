using System;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UI.CarCustomizeWindow;
using UI.CarInspector;
using UI.Common;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("UpdateCarCustomizeWindow")]
public static class UpdateCarCustomizeWindow
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "Populate")]
    public static void Populate(CarInspector __instance, Car car) {
        var carCustomizeWindow = WindowManager.Shared!.GetWindow<CarCustomizeWindow>();
        var window             = carCustomizeWindow?.GetComponent<Window>();
        if (window != null && window.IsShown && CarCustomizeWindow.CanCustomize(car, out _)) {
            __instance.ShowCustomize();
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), nameof(ShowCustomize))]
    private static void ShowCustomize(this CarInspector carInspector) => throw new NotImplementedException("It's a stub: CarInspector.ShowCustomize");
}
