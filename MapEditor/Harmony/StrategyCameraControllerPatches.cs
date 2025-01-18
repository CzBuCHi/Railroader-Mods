using Cameras;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace MapEditor.Harmony;

[PublicAPI]
[HarmonyPatch]
public static class StrategyCameraControllerPatches
{
    public static bool DisableCameraMouseMove;
    public static bool DisableCameraMouseZoom;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StrategyCameraController), nameof(UpdateInput))]
    public static void UpdateInput(ref Vector3? ____panStartPosition, ref float ____distanceInput) {
        if (DisableCameraMouseMove) {
            ____panStartPosition = null;
        }

        if (DisableCameraMouseZoom) {
            ____distanceInput = 0f;
        }
    }
}
