using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Model;
using UI.CarInspector;

namespace CarInspectorTweaks.Harmony;

[HarmonyPatchCategory("ConsistWindow")]
public static class CarInspectorPatches
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), "TitleForCar")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static string TitleForCar(Car car) => throw new NotImplementedException("It's a stub: CarInspector.TitleForCar");

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), "SubtitleForCar")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static string SubtitleForCar(Car car) => throw new NotImplementedException("It's a stub: CarInspector.SubtitleForCar");
}
