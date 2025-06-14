using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Features.AutoTrestles;
using Track;

namespace MapEditor.Features.TrackSegments;

[PublicAPI]
[HarmonyPatch]
public static class TrackSegmentPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TrackSegment), nameof(RebuildBezier))]
    public static void RebuildBezier(TrackSegment __instance) {
        if (MapEditorPlugin.PatchEditor == null) {
            return;
        }

        var visualizer = __instance.GetComponentInChildren<TrackSegmentVisualizer>();
        if (visualizer != null) {
            visualizer.RebuildBezier();
        }

        if (__instance.style == TrackSegment.Style.Bridge) {
            new AutoTrestleUpdate(__instance.id).Do();
        }
    }
}
