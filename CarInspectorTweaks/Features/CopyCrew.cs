using System.Linq;
using Game;
using Game.Messages;
using Game.State;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.Definition;
using Model.Ops;
using Model.Ops.Timetable;
using UI.Builder;
using UI.CompanyWindow;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("CopyCrew")]
public static class CopyCrew
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuilderExtensions), nameof(AddTrainCrewDropdown), typeof(UIPanelBuilder), typeof(Car))]
    public static void AddTrainCrewDropdown(UIPanelBuilder builder, Car car, bool __result) {
        if (__result == false) {
            return;
        }

        builder.ButtonStrip(strip => {
            strip.AddButton("<sprite name=Copy><sprite name=Coupled>", SetCarTrainCrew)!
                 .Tooltip("Copy crew", "Copy this car's crew to the other cars in consist.");
        });
        return;

        void SetCarTrainCrew() {
            car.EnumerateCoupled(Car.End.F)!
               .Where(o => o != car && o.Archetype is CarArchetype.Coach or CarArchetype.LocomotiveDiesel or CarArchetype.LocomotiveSteam or CarArchetype.Caboose)
               .Do(o => StateManager.ApplyLocal(new SetCarTrainCrew(o.id, car.trainCrewId)));

            builder.Rebuild(); 
        }
    }

    private static void CopyStopsFromTimetable(Car car, Timetable.Train timetableTrain) {
        var destinations = (car.GetPassengerMarker() ?? PassengerMarker.Empty()).Destinations!;
        var shared       = TimetableController.Shared;
        var now          = TimeWeather.Now;
        for (var index = 0; index < timetableTrain.Entries.Count; ++index) {
            var entry = timetableTrain.Entries[index];
            if (!(timetableTrain.GetGameDateTimeDeparture(index, now) < now) && shared.TryGetPassengerStop(entry.Station, out var passengerStop)) {
                destinations.Add(passengerStop.identifier);
            }
        }

        var destinationsList = destinations.ToList();

        foreach (var coupled in car.EnumerateCoupled(Car.End.F)) {
            if (coupled.IsPassengerCar()) {
                StateManager.ApplyLocal(new SetPassengerDestinations(coupled.id, destinationsList));
            }
        }
    }
}
