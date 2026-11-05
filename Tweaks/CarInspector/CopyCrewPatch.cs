using System.Linq;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Model.Ops;
using Model;
using UI.Builder;
using UI.CompanyWindow;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.CarInspector
{
    [HarmonyPatch(typeof(BuilderExtensions), nameof(BuilderExtensions.AddTrainCrewDropdown), typeof(UIPanelBuilder), typeof(Car))]
    internal static class CopyCrewPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => Settings.Current.CopyCrew;

        [HarmonyPostfix]
        private static void Postfix(UIPanelBuilder builder, Car car, bool __result)
        {
            if (!__result || car == null) return;
            if (!car.EnumerateCoupled().Any(target => Eligible(target, car) && target.trainCrewId != car.trainCrewId)) return;
            builder.AddButtonCompact(TextSprites.CopyToCoupled, () =>
            {
                if (car == null) return;
                var crew = car.trainCrewId;
                foreach (var target in car.EnumerateCoupled().ToArray())
                {
                    if (!Eligible(target, car) || target.trainCrewId == crew) continue;
                    var message = new SetCarTrainCrew(target.id, crew);
                    if (StateManager.CheckAuthorizedToSendMessage(message)) StateManager.ApplyLocal(message);
                }
                builder.Rebuild();
            }).Width(28).Height(24)
                .Disable(!StateManager.CheckAuthorizedToSendMessage(new SetCarTrainCrew(car.id, car.trainCrewId)))
                .Tooltip("Copy crew", "Copy to coupled locomotives, passenger cars and cabooses. None clears their assignments.");
        }

        private static bool Eligible(Car target, Car source) => target != null && target != source && target.SupportsTrainCrew &&
            (target is BaseLocomotive || target.IsPassengerCar() || target.CarType.IsCaboose);
    }
}
