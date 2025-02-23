using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CarInspectorTweaks.Harmony;
using CzBuCHi.Shared.UI;
using Game.Events;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.AI;
using Model.Definition;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UI.EngineControls;
using UnityEngine;

namespace CarInspectorTweaks.UI;

public sealed class ConsistWindow : ProgrammaticWindowBase
{
    public override Window.Sizing Sizing => Window.Sizing.Fixed(new Vector2Int(300, 150));

    public static ConsistWindow Shared => WindowManager.Shared!.GetWindow<ConsistWindow>()!;

    public override void Awake() {
        base.Awake();
        Window.Title = "Consist Window";
    }

    protected override void WindowOnOnShownDidChange(bool isShown) {
        base.WindowOnOnShownDidChange(isShown);
        if (isShown) {
            var rectTransform = Window.GetComponent<RectTransform>()!;
            rectTransform.position = new Vector2(315, 0).Round();
        }
    }

    protected override void Build(UIPanelBuilder builder) {
        builder.RebuildOnEvent<SelectedCarChanged>();

        var locomotive = TrainController.Shared.SelectedLocomotive;
        if (locomotive == null) {
            builder.AddLabel("No locomotive selected.");
            return;
        }

        builder.AddTitle(CarInspectorPatches.TitleForCar(locomotive), CarInspectorPatches.SubtitleForCar(locomotive));

        var persistence = new AutoEngineerPersistence(locomotive.KeyValueObject!);
        var helper      = new AutoEngineerOrdersHelper(locomotive, persistence);
        var mode        = helper.Mode;

        builder.ButtonStrip(strip => {
            var cars = locomotive.EnumerateCoupled()!.ToList();

            var lowOilCar           = cars[0]!;
            cars.Do(car => {
                strip.AddObserver(car.KeyValueObject!.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Handbrake)!, _ => strip.Rebuild(), false)!);
                strip.AddObserver(car.KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.CylinderCock)!, _ => strip.Rebuild(), false)!);
                strip.AddObserver(car.KeyValueObject.Observe("oiled", _ => strip.Rebuild(), false)!);

                if (lowOilCar.Oiled > car.Oiled) {
                    lowOilCar = car;
                }
            });

            strip.AddButton("Refresh", strip.Rebuild)
                 .Tooltip("Refresh dialog", "Refreshes this dialog");

            if (cars.Any(c => c.air!.handbrakeApplied)) {
                strip.AddButton($"{TextSprites.HandbrakeWheel}", () => {
                         ReleaseAllHandbrakes(cars);
                         strip.Rebuild();
                     })
                     .Tooltip("Release handbrakes", $"Iterates over cars in this consist and releases {TextSprites.HandbrakeWheel}.");
            }

            if (!IsAirConnected(cars)) {
                strip.AddButton("Fix Air", () => {
                         ConnectAir(cars);
                         strip.Rebuild();
                     })
                     .Tooltip("Connect Consist Air", "Iterates over each car in this consist and connects gladhands and opens anglecocks.");
            }

            if (lowOilCar.Oiled < CarInspectorTweaksPlugin.Settings.OilThreshold) {
                strip.AddButton($"Low oil: {lowOilCar.Oiled:0%})", () => CameraSelector.shared!.FollowCar(lowOilCar))!
                     .Tooltip("Jump to low oil car", "Jump the overhead camera to car with lowest oil in bearing.");
            }
        });

        builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("Manual", mode == AutoEngineerMode.Off, UpdateMode(AutoEngineerMode.Off));
            strip.AddButtonSelectable("Road", mode == AutoEngineerMode.Road, UpdateMode(AutoEngineerMode.Road));
            strip.AddButtonSelectable("Yard", mode == AutoEngineerMode.Yard, UpdateMode(AutoEngineerMode.Yard));
            strip.AddButtonSelectable("WP", mode == AutoEngineerMode.Waypoint, UpdateMode(AutoEngineerMode.Waypoint));
        });
        return;

        Action UpdateMode(AutoEngineerMode newMode) {
            return () => {
                helper.SetOrdersValue(newMode);
                builder.Rebuild();
            };
        }
    }

    private static bool IsAirConnected(List<Car> consist) {
        return consist.All(car => IsAirConnectedImpl(car, car.EndGearA!, Car.LogicalEnd.A) && IsAirConnectedImpl(car, car.EndGearB!, Car.LogicalEnd.B));

        bool IsAirConnectedImpl(Car car, Car.EndGear endGear, Car.LogicalEnd end) {
            return car.CoupledTo(end)
                ? car.AirConnectedTo(end) != null && !(endGear.AnglecockSetting < 0.999f)
                : !(endGear.AnglecockSetting > 0.001f);
        }
    }

    private static void ConnectAir(List<Car> consist) {
        foreach (var car in consist) {
            ConnectAirImpl(car, Car.LogicalEnd.A);
            ConnectAirImpl(car, Car.LogicalEnd.B);
        }

        return;

        static void ConnectAirImpl(Car car, Car.LogicalEnd end) {
            StateManager.ApplyLocal(new PropertyChange(car.id!, CarPatches.KeyValueKeyFor(Car.EndGearStateKey.Anglecock, car.LogicalToEnd(end)), new FloatPropertyValue(car[end]!.IsCoupled ? 1f : 0f)));

            if (car.TryGetAdjacentCar(end, out var car2)) {
                StateManager.ApplyLocal(new SetGladhandsConnected(car.id!, car2!.id!, true));
            }
        }
    }

    private static void ReleaseAllHandbrakes(List<Car> consist) {
        consist.Do(c => c.SetHandbrake(false));
    }

}


