using HarmonyLib;
using Model;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;

namespace Tweaks
{
    [HarmonyPatch(typeof(CarInspector), "PopulateCarPanel")]
    internal static class CarPanelPatch
    {
        [HarmonyPrefix]
        private static void Prefix(UIPanelBuilder builder, Car ____car)
        {
            var car = ____car;
            if (car == null) return;
            if (!(car is BaseLocomotive))
                builder.AddField("Speed", () => car != null ? Mathf.RoundToInt(car.VelocityMphAbs) + " MPH" : "—", UIPanelBuilder.Frequency.Periodic);
            if (car.EnableOiling)
                builder.AddField("Oiled", () => car != null && car.EnableOiling ? Mathf.RoundToInt(car.Oiled * 100f) + "%" : "—", UIPanelBuilder.Frequency.Periodic);
        }
    }
}
