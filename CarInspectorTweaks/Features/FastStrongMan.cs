using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Serilog;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("FastStrongMan")]
public static class FastStrongMan
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TrainController), nameof(TrainController.HandleManualMoveCar))]
    public static IEnumerable<CodeInstruction> HandleManualMoveCar(IEnumerable<CodeInstruction> instructions) {
        var codeInstructions = instructions.ToArray();

        var inst = codeInstructions[codeInstructions.Length - 3];
        if (inst.opcode != OpCodes.Ldc_R4) {
            Log.Error("TrainController.HandleManualMoveCar has changed - 'FastStrongMan' disabled");
        } else {
            inst.operand = 2.682236f;
        }

        return codeInstructions;
    }
}
