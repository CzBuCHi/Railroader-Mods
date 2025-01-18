using Model;

namespace CarInspectorTweaks.Features;

using HarmonyLib;
using JetBrains.Annotations;
using Model.Definition.Components;
using Model.Definition.Data;
using RollingStock.Steam;
using System.Linq;
using UI.Builder;
using UI.CarCustomizeWindow;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("WhistleButtons")]
public static class WhistleButtons
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarCustomizeWindow), nameof(BuildSoundTabWhistle))]
    public static void BuildSoundTabWhistle(UIPanelBuilder builder, WhistleComponent whistleComponent, Car ____car) {
        var settings = WhistleCustomizationSettings.FromPropertyValue(____car.KeyValueObject!["whistle.custom"])
                       ?? new WhistleCustomizationSettings(whistleComponent.DefaultWhistleIdentifier!);

        var whistles     = TrainController.Shared!.PrefabStore!.AllDefinitionInfosOfType<WhistleDefinition>()!.ToList();
        var currentIndex = whistles.FindIndex(id => settings.WhistleIdentifier == id.Identifier);
        builder.AddField("",
            builder.ButtonStrip(strip => {
                strip.AddButton("Previous", () => UpdateWhistle(--currentIndex))!.Disable(currentIndex == 0);
                strip.AddButton("Next", () => UpdateWhistle(++currentIndex))!.Disable(currentIndex == whistles.Count - 1);
            })!
        );

        return;

        void UpdateWhistle(int index) {
            ____car.KeyValueObject["whistle.custom"] = new WhistleCustomizationSettings(whistles[index]!.Identifier!).PropertyValue;
            builder.Rebuild();
        }
    }
}