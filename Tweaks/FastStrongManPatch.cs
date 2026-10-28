using System.Collections.Generic;
using HarmonyLib;

namespace Tweaks
{
    [HarmonyPatch(typeof(TrainController), nameof(TrainController.HandleManualMoveCar))]
    internal static class FastStrongManPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.FasterManualPushing;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ManualPushSpeed.Apply(instructions);
    }
}
