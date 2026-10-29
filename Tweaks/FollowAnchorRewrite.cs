using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Tweaks
{
    internal static class FollowAnchorRewrite
    {
        internal static IEnumerable<CodeInstruction> Apply(IEnumerable<CodeInstruction> instructions, MethodInfo anchor, MethodInfo adjust)
        {
            var code = instructions.ToList();
            var matches = code.Count(i => i.Calls(anchor));
            if (matches != 1) throw new InvalidOperationException("Unexpected native follow-camera anchor count: " + matches);
            foreach (var instruction in code)
            {
                yield return instruction;
                if (!instruction.Calls(anchor)) continue;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, adjust);
            }
        }
    }
}
