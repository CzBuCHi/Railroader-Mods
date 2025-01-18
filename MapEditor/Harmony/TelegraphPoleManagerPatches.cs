using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using MapEditor.Visualizers;
using TelegraphPoles;

namespace MapEditor.Harmony;

[PublicAPI]
[HarmonyPatch]
internal static class TelegraphPoleManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TelegraphPoleManager), nameof(BuildPole))]
    public static void BuildPole(SimpleGraph.Runtime.SimpleGraph graph, SimpleGraph.Runtime.Node node, Dictionary<int, TelegraphPole> ____instances)
    {
        var telegraphPole = ____instances[node.id]!;
        telegraphPole.gameObject.SetActive(false);
        var visualizer = telegraphPole.gameObject.AddComponent<TelegraphPoleVisualizer>();
        visualizer.NodeId = node.id;
        telegraphPole.gameObject.SetActive(true);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(TelegraphPoleManager), nameof(Rebuild))]
    public static void Rebuild(this TelegraphPoleManager __instance)
    {
    }
}
