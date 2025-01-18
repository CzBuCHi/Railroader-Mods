using Game.Messages;
using HarmonyLib;
using JetBrains.Annotations;
using Model.AI;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("YardMaxSpeed")]
public static class YardMaxSpeed
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AutoEngineerOrdersExtensions), nameof(MaxSpeedMph))]
    public static bool MaxSpeedMph(AutoEngineerMode mode, ref int __result) {
        if (mode == AutoEngineerMode.Yard) {
            __result = CarInspectorTweaksPlugin.Settings.YardMaxSpeed;
            return false;
        }

        return true;
    }
}
