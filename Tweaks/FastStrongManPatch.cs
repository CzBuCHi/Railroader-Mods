using System.Collections.Generic;
using HarmonyLib;

namespace Tweaks
{
    [HarmonyPatch(typeof(TrainController), nameof(TrainController.HandleManualMoveCar))]
    internal static class FastStrongManPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ManualPushSpeed.Apply(instructions);
    }
}
