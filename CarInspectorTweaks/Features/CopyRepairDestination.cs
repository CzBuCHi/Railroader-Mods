using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.Ops;
using UI.Builder;
using UI.CarInspector;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("CopyRepairDestination")]
public static class CopyRepairDestinationTranspiler
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CarInspector), "PopulateEquipmentPanel")]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original) {
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.MatchEndForward(
                       CodeMatch.Calls(() => default(UIPanelBuilder).AddExpandingVerticalSpacer())
                   )
                   .ThrowIfInvalid("Could not find any call to UIPanelBuilder.AddExpandingVerticalSpacer")
                   .Advance(1)
                   .RemoveInstructions(instructions.Count() - codeMatcher.Pos - 1);
        return codeMatcher.Instructions();
    }
}

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("CopyRepairDestination")]
public static class CopyRepairDestination
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), nameof(PopulateEquipmentPanel))]
    public static void PopulateEquipmentPanel(CarInspector __instance, UIPanelBuilder builder, Car ____car) {
        builder.ButtonStrip(strip => {
            var canCustomize = __instance.CanCustomize(out var reason);
            if (canCustomize || !string.IsNullOrEmpty(reason)) {
                var customize = strip.AddButton("Customize", __instance.ShowCustomize)!;
                if (!canCustomize) {
                    customize.Disable(true)!
                             .Tooltip("Customize Not Available", reason);
                }
            }

            strip.AddButton("<sprite name=Copy><sprite name=Coupled>", () => {
                ____car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared!, out var result);
                ____car.EnumerateCoupled(Car.End.F)!
                       .Where(o => o != ____car)
                       .Do(o => o.SetOverrideDestination(OverrideDestination.Repair, result));

                builder.Rebuild();
            })!.Tooltip("Copy repair destination", "Copy this car's repair destination to the other cars in consist.");
        });
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), nameof(CanCustomize))]
    private static bool CanCustomize(this CarInspector carInspector, out string reason) => throw new NotImplementedException("It's a stub: CarInspector.CanCustomize");

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), nameof(ShowCustomize))]
    private static void ShowCustomize(this CarInspector carInspector) => throw new NotImplementedException("It's a stub: CarInspector.ShowCustomize");

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(RepairTrack), nameof(CalculateRepairWorkOverall))]
    private static float CalculateRepairWorkOverall(Car car) => throw new NotImplementedException("It's a stub: RepairTrack.CalculateRepairWorkOverall");
}
