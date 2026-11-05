using System;
using HarmonyLib;
using Relay;
using UnityEngine;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Units
{
    [HarmonyPatch(typeof(FileSystemModuleLoader), nameof(FileSystemModuleLoader.LoadModuleSource))]
    internal static class TutorialDistancePatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.UseMeters;

        [HarmonyPostfix]
        private static void Postfix(string displayName, ref string __result)
        {
            if (__result == null || !TutorialDistanceSource.IsTutorialContext(displayName, Application.streamingAssetsPath)) return;
            try { __result = TutorialDistanceSource.InMeters(__result); }
            catch (NotSupportedException error) { Debug.LogWarning("[Tweaks] " + error.Message); }
        }
    }
}
