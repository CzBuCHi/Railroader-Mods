using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Tweaks;

[HarmonyPatch(typeof(TrainController), nameof(TrainController.HandleManualMoveCar))]
internal static class FastStrongManPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var matches = new List<int>();
        for (var i = 0; i + 1 < code.Count; i++)
            if (code[i].opcode == OpCodes.Ldc_R4 && code[i].operand is float value && Math.Abs(value - 1.3411179f) < 0.000001f &&
                (code[i + 1].opcode == OpCodes.Call || code[i + 1].opcode == OpCodes.Callvirt) &&
                code[i + 1].operand is MethodInfo method && method.Name == "AddVelocityToCar")
                matches.Add(i);
        if (matches.Count != 1)
            throw new NotSupportedException("Tweaks: expected one native manual-push speed limit before AddVelocityToCar.");
        code.Insert(matches[0] + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FastStrongManPatch), nameof(SpeedLimit))));
        return code;
    }
    private static float SpeedLimit(float original) => TweaksPlugin.Settings.FastStrongMan ? 2.682236f : original;
}