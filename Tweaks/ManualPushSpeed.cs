using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Tweaks
{
    internal static class ManualPushSpeed
    {
        internal const float NativeLimit = 1.3411179f;
        internal const float NewLimit = 2.682236f;

        internal static IEnumerable<CodeInstruction> Apply(IEnumerable<CodeInstruction> instructions)
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
            code[matches[0]].operand = NewLimit;
            return code;
        }
    }
}
