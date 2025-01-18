using HarmonyLib;
using JetBrains.Annotations;
using Model;
using UI.CarInspector;
using UI.Common;
using UnityEngine;

namespace CarInspectorTweaks.Features;

[PublicAPI]
[HarmonyPatch]
[HarmonyPatchCategory("CarInspectorHeight")]
public class CarInspectorHeight
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CarInspector), "Show")]
    public static void Show(Car car, CarInspector ____instance) {
        var window = ____instance.GetComponent<Window>();
        var size = window.GetContentSize();
        window.SetContentSize(new Vector2(size.x - 2, CarInspectorTweaksPlugin.Settings.CarInspectorHeight));
    }
}
