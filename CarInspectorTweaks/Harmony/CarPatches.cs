using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Model;

namespace CarInspectorTweaks.Harmony;

[HarmonyPatchCategory("ConsistWindow")]
public static class CarPatches
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end) => throw new NotImplementedException("It's a stub: Car.KeyValueKeyFor");
}