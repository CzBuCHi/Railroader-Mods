using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System;
using HarmonyLib;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.ManualPushing
{
    [HarmonyPatch(typeof(TrainController), nameof(TrainController.HandleManualMoveCar))]
    internal static class FastStrongManPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.FasterManualPushing;

        internal const float NativeLimit = 1.3411179f;
        internal const float NewLimit = 2.682236f;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
            var matches = new List<int>();
            for (var i = 0; i + 1 < code.Count; i++)
                if (code[i].opcode == OpCodes.Ldc_R4 && code[i].operand is float value && Math.Abs(value - NativeLimit) < 0.000001f &&
                    (code[i + 1].opcode == OpCodes.Call || code[i + 1].opcode == OpCodes.Callvirt) &&
                    code[i + 1].operand is MethodInfo method && method.Name == "AddVelocityToCar")
                    matches.Add(i);
            if (matches.Count != 1)
                throw new NotSupportedException("Tweaks: expected exactly one native manual-push limit before AddVelocityToCar.");
            // Change only the cap. Native force/weight, host authority and collision code remain intact.
            code[matches[0]].operand = NewLimit * (Settings.Current.ManualPushSpeedMph / 6f);
            return code;
        }
    }
}
