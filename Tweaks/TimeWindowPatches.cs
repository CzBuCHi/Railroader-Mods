using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Game;
using HarmonyLib;
using Model.Ops;
using UI;

namespace Tweaks;

[HarmonyPatch(typeof(TimeWindow), "GetSleepValues")]
internal static class SleepUntilInterchangePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ref GameDateTime now, ref int targetHour, ref float hoursToSleep)
    {
        if (!TweaksPlugin.Settings.SleepUntilNextInterchange || OpsController.Shared == null)
            return true;
        var current = TimeWeather.Now;
        var times = OpsController.Shared.EnabledInterchanges.Where(interchange => interchange != null)
            .Select(interchange => interchange.GetNextServiceTime(current, out _).TotalSeconds);
        var target = NextServiceSelection.Find(current.TotalSeconds, times);
        if (!target.HasValue) return true;
        now = current;
        hoursToSleep = (float)((target.Value - current.TotalSeconds) / 3600.0);
        targetHour = (int)Math.Floor(current.AddingHours(hoursToSleep).Hours);
        return false;
    }
}

[HarmonyPatch]
internal static class SleepTooltipPatch
{
    // Find native tooltip-bearing code by its literal, independent of lambda numbering.
    private const string Original = "Let time pass until the first scheduled interchange service of the day.";
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> Targets()
    {
        foreach (var method in typeof(TimeWindow).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            var bytes = method.GetMethodBody()?.GetILAsByteArray();
            if (bytes == null) continue;
            for (var i = 0; i + 4 < bytes.Length; i++)
            {
                if (bytes[i] != 0x72) continue;
                string? literal = null;
                try { literal = method.Module.ResolveString(BitConverter.ToInt32(bytes, i + 1)); }
                catch (ArgumentException) { }
                if (literal == Original) { yield return method; break; }
            }
        }
    }
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string text && text == Original)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SleepTooltipPatch), nameof(Tooltip))) {
                    labels = instruction.labels, blocks = instruction.blocks
                };
            }
            else yield return instruction;
        }
    }
    private static string Tooltip() => TweaksPlugin.Settings.SleepUntilNextInterchange
        ? "Let time pass until the next enabled interchange service, including scheduled extra service."
        : Original;
}