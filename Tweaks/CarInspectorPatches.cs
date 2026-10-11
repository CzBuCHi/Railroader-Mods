using System.Linq;
using HarmonyLib;
using Model;
using Model.Ops;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch(typeof(CarInspector), "PopulateCarPanel")]
internal static class CarPanelPatch
{
    [HarmonyPrefix]
    private static void Prefix(UIPanelBuilder builder, Car ____car)
    {
        var car = ____car;
        if (car == null) return;
        var settings = TweaksPlugin.Settings;
        if (settings.ShowCarSpeed && !(car is BaseLocomotive))
            builder.AddField("Speed", () => car != null ? Mathf.RoundToInt(car.VelocityMphAbs).ToString() + " MPH" : "—", UIPanelBuilder.Frequency.Periodic);
        if (settings.ShowCarOil && car.EnableOiling)
            builder.AddField("Oiled", () => car != null ? Mathf.RoundToInt(car.Oiled * 100f) + "%" : "—", UIPanelBuilder.Frequency.Periodic);

    }
}

[HarmonyPatch(typeof(CarInspector), "PopulateEquipmentPanel")]
internal static class RepairDestinationPatch
{
    [HarmonyPostfix]
    private static void Postfix(UIPanelBuilder builder, Car ____car)
    {
        var car = ____car;
        if (!TweaksPlugin.Settings.CopyRepairDestination || car == null || OpsController.Shared == null ||
            !car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared, out _))
            return;
        builder.AddButtonCompact(TextSprites.CopyToCoupled, () => {
            if (car == null || OpsController.Shared == null ||
                !car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared, out var destination))
                return;
            foreach (var coupled in car.EnumerateCoupled().ToArray())
                if (coupled != null && coupled != car)
                    coupled.SetOverrideDestination(OverrideDestination.Repair, destination);
            builder.Rebuild();
        }).Tooltip("Copy repair destination", "Copy this car's selected repair destination to every other coupled car.");
    }
}