using HarmonyLib;
using UnityEngine;

namespace TurntableControl;

// Reuse native picking, including its distance, occlusion and UI handling.
[HarmonyPatch(typeof(ObjectPicker), "Update")]
internal static class ObjectPickerPatch
{
    [HarmonyPostfix]
    private static void Postfix(ObjectPicker __instance, bool ____mouseOverUI)
    {
        var target = !____mouseOverUI && Cursor.lockState != CursorLockMode.Locked
            ? __instance.HoveringOver as TurntableMarker
            : null;
        TurntableMarker.SetHovered(target);
    }
}
