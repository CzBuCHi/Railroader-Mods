using HarmonyLib;
using Model;

namespace Tweaks
{
    [HarmonyPatch(typeof(Units), nameof(Units.DistanceText))]
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
