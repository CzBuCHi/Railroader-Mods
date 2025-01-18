using System;
using HarmonyLib;
using JetBrains.Annotations;

namespace Teleporter.Harmony;

using static CameraSelector;

[PublicAPI]
[HarmonyPatch]
public static class CameraSelectorPatches {
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CameraSelector), "SelectCamera")]
    public static bool SelectCamera(this CameraSelector __instance, CameraIdentifier cameraIdentifier) =>
        throw new NotImplementedException("It's a stub: CameraSelector.SelectCamera");
}
