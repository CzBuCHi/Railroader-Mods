using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CzBuCHi.Shared.UI;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using HarmonyLib;
using Model;
using Serilog;
using UI.Builder;
using UI.CarInspector;
using UI.Common;
using UnityEngine;

namespace CarInspectorTweaks.UI;

public class ConsistWindow : ProgrammaticWindowBase
{
    public override Window.Sizing Sizing => Window.Sizing.Fixed(new Vector2Int(700, 400));

    private readonly UIState<string?> _SelectedItem = new(null);

    public static ConsistWindow Shared => WindowManager.Shared!.GetWindow<ConsistWindow>()!;

    private BaseLocomotive? _Locomotive;
    protected override void Build(UIPanelBuilder builder) {
        //builder.RebuildOnEvent<SelectedCarChanged>();
        builder.RebuildOnInterval(1);
        
        var locomotive = TrainController.Shared.SelectedLocomotive;
        if (locomotive == null) {
            builder.AddLabel("No locomotive selected.");
            return;
        }

        if (_Locomotive != locomotive) {
            _SelectedItem.Value = locomotive.id;
            _Locomotive = locomotive;
        }

        var cars = locomotive.EnumerateCoupled().ToList();
        Window.Title = "Train " + locomotive.DisplayName;
        builder.AddListDetail(cars.Select(GetListItem), _SelectedItem, BuildDetail);
    }

    private UIPanelBuilder.ListItem<Car> GetListItem(Car car) {
        var isAirConnected = !CheckAir(car, car.EndGearA!, Car.LogicalEnd.A) || !CheckAir(car, car.EndGearB!, Car.LogicalEnd.B);

        var displayName = car.DisplayName +
                          (isAirConnected ? " " + TextSprites.Warning : "") +
                          (car.air!.handbrakeApplied ? " " + TextSprites.HandbrakeWheel : "") +
                          (car.NeedsOiling ? " " + TextSprites.Hotbox : "");

        return new UIPanelBuilder.ListItem<Car>(car.id, car, car.Archetype.ToString(), displayName);
    }

    private static bool CheckAir(Car car, Car.EndGear endGear, Car.LogicalEnd end) {
        if (car.CoupledTo(end)) {
            if (car.AirConnectedTo(end) == null || endGear.AnglecockSetting < 0.999f) {
                return false;
            }
        } else {
            if (endGear.AnglecockSetting > 0.001f) {
                return false;
            }
        }

        return true;
    }

    private void BuildDetail(UIPanelBuilder builder, Car? car) {
        if (car == null) {
            builder.AddLabel("No car selected.");
            return;
        }

        var setter = _CarSetter ??= CreatePrivateSetter();
        var carInspector = new CarInspector();
        setter(carInspector, car);
        carInspector.PopulatePanel(builder);
    }

    private static Action<CarInspector, Car>? _CarSetter;

    private static Action<CarInspector, Car> CreatePrivateSetter() {
        var fieldInfo = typeof(CarInspector).GetField("_car", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null) {
            throw new InvalidOperationException("Field '_car' not found!");
        }

        var instanceParam    = Expression.Parameter(typeof(CarInspector), "instance");
        var valueParam       = Expression.Parameter(typeof(Car), "value");
        var fieldAccess      = Expression.Field(instanceParam, fieldInfo);
        var assignExpression = Expression.Assign(fieldAccess, valueParam);
        var setterLambda     = Expression.Lambda<Action<CarInspector, Car>>(assignExpression, instanceParam, valueParam);
        return setterLambda.Compile();
    }
}

[HarmonyPatchCategory("ConsistWindow")]
public static class CarInspectorPatches
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CarInspector), "PopulatePanel")]
    public static void PopulatePanel(this CarInspector carInspector, UIPanelBuilder builder) => throw new NotImplementedException("It's a stub: CarInspector.PopulatePanel");
}


