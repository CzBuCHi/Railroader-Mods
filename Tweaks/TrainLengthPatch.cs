using System.Linq;
using HarmonyLib;
using Model;
using UI;

namespace Tweaks
{
    [HarmonyPatch(typeof(LocomotiveControlsHoverArea), nameof(LocomotiveControlsHoverArea.SummaryText))]
    internal static class TrainLengthPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.ShowTrainLengthInMeters;

        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            if (TrainController.Shared == null) return;
            // Use the native length in meters, avoiding a round trip through rounded feet.
            var cars = TrainController.Shared.SelectedTrain.ToList();
            __result = TrainLengthDisplay.InMeters(__result, ConsistLength.OfCars(cars));
        }
    }
}
