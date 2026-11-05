using HarmonyLib;
using Model;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Units
{
    [HarmonyPatch(typeof(Model.Units), nameof(Model.Units.DistanceText))]
    internal static class DistanceTextPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.UseMeters;

        [HarmonyPrefix]
        private static bool Prefix(float distanceInMeters, ref string __result)
        {
            var text = DistanceDisplay.InMeters(distanceInMeters);
            if (text == null) return true;
            __result = text;
            return false;
        }
    }
}
