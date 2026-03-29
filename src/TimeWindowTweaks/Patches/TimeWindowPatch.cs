using System;
using Game;
using Game.State;
using HarmonyLib;
using JetBrains.Annotations;
using Model.Ops;
using UI;

namespace TimeWindowTweaks.Patches
{
    [PublicAPI]
    [HarmonyPatch]
    public static class TimeWindowPatch
    {
        /// <summary>
        /// This patch changes the sleep time calculation to align with the next interchange service time, ensuring that players wake up in time for the next available service.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TimeWindow), "GetSleepValues")]
        public static bool GetSleepValues(out GameDateTime now, out int targetHour, out float hoursToSleep) {
            now = TimeWeather.Now;

            // find next interchange service time
            GameDateTime? targetTime = null;
            foreach (var interchange in OpsController.Shared!.EnabledInterchanges!) {
                var serviceTime = interchange.GetNextServiceTime(now, out _);
                if (targetTime == null || targetTime > serviceTime) {
                    targetTime = serviceTime;
                }
            }

            var targetTimeHours = targetTime?.Hours ?? StateManager.Shared!.Storage!.InterchangeServeHour;

            hoursToSleep = now.Hours < targetTimeHours 
                ? targetTimeHours  - now.Hours
                : 24f - now.Hours + targetTimeHours ;
            targetHour = (int)Math.Floor(targetTimeHours );

            return false; // Harmony Prefix: skip original
        }
    }
}
