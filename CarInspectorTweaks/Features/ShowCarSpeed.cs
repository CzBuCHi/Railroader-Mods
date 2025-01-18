using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("ShowCarSpeed")]
public static class ShowCarSpeed
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarInspector), "PopulateCarPanel")]
    public static void PopulateCarPanelPrefix(UIPanelBuilder builder, Car ____car) {
        if (____car is BaseLocomotive) {
            return;
        }

        builder.AddField("Speed", () => {
            var velocityMphAbs = ____car.VelocityMphAbs;
            velocityMphAbs = velocityMphAbs >= 1.0 ? Mathf.RoundToInt(velocityMphAbs) : velocityMphAbs > 0.10000000149011612 ? 1f : 0.0f;
            return velocityMphAbs + " MPH";
        }, UIPanelBuilder.Frequency.Periodic);
    }
}