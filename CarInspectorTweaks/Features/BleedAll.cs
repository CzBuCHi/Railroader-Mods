using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UI.Builder;
using UI.CarInspector;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("BleedAll")]
public static class BleedAll
{
    [HarmonyPostfix]
    public static void Postfix(UIPanelBuilder field, object __instance) {
        var closure      = Traverse.Create(__instance)!.Field("<>4__this")!.GetValue()!;
        var car          = Traverse.Create(closure)!.Field<Car>("_car")!.Value!;
        var bleedAllCars = BleedAllCars(car);
        field.AddButtonCompact("All", bleedAllCars)!
             .Tooltip("Bleed All Valves", "Bleed the brakes to release pressure from the train's brake system.");
    }

    private static Action BleedAllCars(Car car) {
        return () => car.EnumerateCoupled()!.Do(c => {
            if (c.SupportsBleed()) {
                c.SetBleed();
            }
        });
    }

    public static MethodBase TargetMethod() {
        var type = typeof(CarInspector).Assembly.GetType("UI.CarInspector.CarInspector+<>c__DisplayClass21_0");
        if (type == null) {
            throw new Exception("Type UI.CarInspector.CarInspector+<>c__DisplayClass21_0 not found.");
        }

        var targetMethod = AccessTools.Method(type, "<PopulateCarPanel>b__4");
        if (targetMethod == null) {
            throw new Exception("Method UI.CarInspector.CarInspector+<>c__DisplayClass21_0::b__4 not found.");
        }

        return targetMethod;
    }
}
