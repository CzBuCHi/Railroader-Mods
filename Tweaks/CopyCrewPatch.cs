using System.Linq;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model;
using Model.Definition;
using Model.Ops;
using UI.Builder;
using UI.CompanyWindow;

namespace Tweaks;

[HarmonyPatch(typeof(BuilderExtensions), nameof(BuilderExtensions.AddTrainCrewDropdown), typeof(UIPanelBuilder), typeof(Car))]
internal static class CopyCrewPatch
{
    [HarmonyPostfix]
    private static void Postfix(UIPanelBuilder builder, Car car, bool __result)
    {
        if (!TweaksPlugin.Settings.CopyCrew || !__result || car == null) return;
        if (!car.EnumerateCoupled().Any(target => Eligible(target, car) && target.trainCrewId != car.trainCrewId)) return;
        builder.AddButtonCompact(TextSprites.CopyToCoupled, () => {
            if (car == null) return;
            // Read both the current crew and consist at click time. None also copies.
            var crew = car.trainCrewId;
            foreach (var target in car.EnumerateCoupled().ToArray())
            {
                if (!Eligible(target, car) || target.trainCrewId == crew) continue;
                var message = new SetCarTrainCrew(target.id, crew);
                if (StateManager.CheckAuthorizedToSendMessage(message)) StateManager.ApplyLocal(message);
            }
            builder.Rebuild();
        }).Disable(!StateManager.CheckAuthorizedToSendMessage(new SetCarTrainCrew(car.id, car.trainCrewId)))
            .Tooltip("Copy crew", "Copy this car's train crew to the coupled locomotives, passenger cars and cabooses. None clears their assignments.");
    }
    private static bool Eligible(Car target, Car source) => target != null && target != source &&
        (target is BaseLocomotive || target.IsPassengerCar() || target.Archetype == CarArchetype.Caboose);
}
