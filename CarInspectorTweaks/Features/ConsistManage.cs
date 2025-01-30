using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CarInspectorTweaks.UI;
using Game.Messages;
using Game.State;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using Model.AI;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UI.EngineControls;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("ConsistManage")]
public static class ConsistManage {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), nameof(PopulateCarPanel))]
    public static void PopulateCarPanel(UIPanelBuilder builder, CarInspector __instance, Car ____car, Window ____window) {
        var persistence = new AutoEngineerPersistence(____car.KeyValueObject!);

        var locomotive  = ____car as BaseLocomotive;
        var helper      = new AutoEngineerOrdersHelper(locomotive, persistence);
        var mode        = helper.Mode;

        builder.ButtonStrip(strip => {
            var cars = ____car.EnumerateCoupled()!.ToList();

            var lowOilCar = cars[0]!;
            cars.Do(car => {
                strip.AddObserver(car.KeyValueObject!.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Handbrake)!, _ => strip.Rebuild(), false)!);
                strip.AddObserver(car.KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.CylinderCock)!, _ => strip.Rebuild(), false)!);
                strip.AddObserver(car.KeyValueObject.Observe("oiled", _ => strip.Rebuild(), false)!);
                if (lowOilCar.Oiled > car.Oiled) {
                    lowOilCar = car;
                }
            });

            strip.AddButton("Refresh", strip.Rebuild)!
                 .Tooltip("Refresh dialog", "Refreshes this dialog");

            if (cars.Any(c => c.air!.handbrakeApplied)) {
                strip.AddButton($"{TextSprites.HandbrakeWheel}", () => {
                    ReleaseAllHandbrakes(cars);
                    strip.Rebuild();
                })!
                     .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
            }

            if (!IsAirConnected(cars)) {
                strip.AddButton("Fix Air", () => {
                    ConnectAir(cars);
                    strip.Rebuild();
                })!
                     .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
            }

            if (cars.Any(o => o.Oiled < CarInspectorTweaksPlugin.Settings.OilThreshold)) {
                strip.AddButton($"Low oil: {lowOilCar.Oiled:0%})", () => CameraSelector.shared!.FollowCar(lowOilCar))!
                     .Tooltip("Jump to low oil car", "Jump the overhead camera to car with lowest oil in bearing.");
            }
        });

        if (locomotive != null) {
            builder.ButtonStrip(strip => {
                strip.AddButtonSelectable("Manual", mode == AutoEngineerMode.Off, UpdateMode(AutoEngineerMode.Off));
                strip.AddButtonSelectable("Road", mode == AutoEngineerMode.Road, UpdateMode(AutoEngineerMode.Road));
                strip.AddButtonSelectable("Yard", mode == AutoEngineerMode.Yard, UpdateMode(AutoEngineerMode.Yard));
                strip.AddButtonSelectable("WP", mode == AutoEngineerMode.Waypoint, UpdateMode(AutoEngineerMode.Waypoint));
            });
        }

        return;

        Action UpdateMode(AutoEngineerMode newMode) {
            return () => {
                helper.SetOrdersValue(newMode);
                builder.Rebuild();
            };
        }
    }

    private static bool IsAirConnected(List<Car> consist) {
        var result = true;
        foreach (var car in consist) {
            CheckAir(car, car.EndGearA!, Car.LogicalEnd.A);
            CheckAir(car, car.EndGearB!, Car.LogicalEnd.B);
        }

        return result;

        void CheckAir(Car car, Car.EndGear endGear, Car.LogicalEnd end) {
            if (car.CoupledTo(end)) {
                if (car.AirConnectedTo(end) == null || endGear.AnglecockSetting < 0.999f) {
                    result = false;
                }
            } else {
                if (endGear.AnglecockSetting > 0.001f) {
                    result = false;
                }
            }
        }
    }

    private static void ConnectAir(List<Car> consist) {
        foreach (var car in consist) {
            ConnectAirCore(car, Car.LogicalEnd.A);
            ConnectAirCore(car, Car.LogicalEnd.B);
        }

        return;

        static void ConnectAirCore(Car car, Car.LogicalEnd end) {
            StateManager.ApplyLocal(new PropertyChange(car.id!, KeyValueKeyFor(Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end)), new FloatPropertyValue(car[end]!.IsCoupled ? 1f : 0f)));

            if (car.TryGetAdjacentCar(end, out var car2)) {
                StateManager.ApplyLocal(new SetGladhandsConnected(car.id!, car2!.id!, true));
            }
        }
    }

    private static void ReleaseAllHandbrakes(List<Car> consist) {
        consist.Do(c => c.SetHandbrake(false));
    }

    private static void OilAllCars(List<Car> consist) {
        consist.Do(c => c.OffsetOiled(1f));
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Car), "KeyValueKeyFor")]
    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    private static string KeyValueKeyFor(Car.EndGearStateKey key, Car.End end) => throw new NotImplementedException("It's a stub: Car.KeyValueKeyFor");
}
