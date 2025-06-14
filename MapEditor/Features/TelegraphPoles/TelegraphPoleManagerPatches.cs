using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SimpleGraph.Runtime;
using TelegraphPoles;

namespace MapEditor.Features.TelegraphPoles;

[PublicAPI]
[HarmonyPatch]
internal static class TelegraphPoleManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TelegraphPoleManager), nameof(BuildPole))]
    public static void BuildPole(SimpleGraph.Runtime.SimpleGraph graph, Node node, Dictionary<int, TelegraphPole> ____instances) {
        var telegraphPole = ____instances[node.id]!;
        telegraphPole.gameObject.SetActive(false);
        var visualizer = telegraphPole.gameObject.AddComponent<TelegraphPoleVisualizer>();
        visualizer.NodeId = node.id;
        telegraphPole.gameObject.SetActive(true);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(TelegraphPoleManager), nameof(Rebuild))]
    public static void Rebuild(this TelegraphPoleManager __instance) {
    }
}
