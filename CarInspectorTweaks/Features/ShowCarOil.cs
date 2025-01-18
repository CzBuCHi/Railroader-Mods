using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("ShowCarOil")]
public static class ShowCarOil
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CarInspector), "PopulateCarPanel")]
    public static void PopulateCarPanelPrefix(UIPanelBuilder builder, Car ____car) {
        if (!____car.EnableOiling) {
            return;
        }

        builder.HStack(hStack => {
            hStack.AddField("Oiled",
                hStack.HStack(field => {
                    field.AddLabel(() => Mathf.RoundToInt(____car.Oiled * 100) + "%", UIPanelBuilder.Frequency.Periodic);
                })!
            );
        });
    }
}
