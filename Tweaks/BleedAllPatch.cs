using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Model;
using UI.Builder;
using UI.CarInspector;

namespace Tweaks;

[HarmonyPatch]
internal static class BleedAllPatch
{
    private static readonly FieldInfo CarField = AccessTools.Field(typeof(CarInspector), "_car");
    private static FieldInfo ownerField = null!;

    [HarmonyTargetMethod]
    private static MethodBase Target()
    {
        // Locate the native cylinder-row builder by its literal, not lambda numbering.
        var methods = typeof(CarInspector).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .SelectMany(type => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(UIPanelBuilder) && HasLiteral(method, "Bleed"))
            .ToArray();
        if (methods.Length != 1)
            throw new NotSupportedException("Tweaks: native bleed row could not be uniquely identified.");
        ownerField = methods[0].DeclaringType!.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .Single(field => field.FieldType == typeof(CarInspector));
        return methods[0];
    }

    private static bool HasLiteral(MethodBase method, string expected)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes == null) return false;
        for (var i = 0; i + 4 < bytes.Length; i++)
        {
            if (bytes[i] != 0x72) continue;
            try { if (method.Module.ResolveString(BitConverter.ToInt32(bytes, i + 1)) == expected) return true; }
            catch (ArgumentException) { }
        }
        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(UIPanelBuilder __0, object __instance)
    {
        if (!TweaksPlugin.Settings.BleedAll) return;
        var inspector = ownerField.GetValue(__instance) as CarInspector;
        var car = inspector == null ? null : CarField.GetValue(inspector) as Car;
        if (car == null || !car.SupportsBleed()) return;
        __0.AddButtonCompact("All", () => {
            if (car == null) return;
            foreach (var coupled in car.EnumerateCoupled().ToArray())
                if (coupled != null && coupled.SupportsBleed()) coupled.SetBleed();
        }).Tooltip("Bleed all valves", "Bleed the brakes on every coupled car that supports a bleed valve.");
    }
}