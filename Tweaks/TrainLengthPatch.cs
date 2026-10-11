using System;
using System.Globalization;
using HarmonyLib;
using UI;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch(typeof(LocomotiveControlsHoverArea), nameof(LocomotiveControlsHoverArea.SummaryText))]
internal static class TrainLengthPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref string __result)
    {
        var separator = __result.LastIndexOf(", ", StringComparison.Ordinal);
        if (separator < 0 || !__result.EndsWith(" ft", StringComparison.Ordinal)) return;
        var nativeLength = __result.Substring(separator + 2, __result.Length - separator - 5);
        if (!int.TryParse(nativeLength, NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture, out var feet)) return;
        var meters = Mathf.RoundToInt(feet * 0.3048f);
        __result = __result.Substring(0, separator + 2) + meters.ToString("N0") + " m";
    }
}
