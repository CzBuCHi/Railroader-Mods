using HarmonyLib;
using TelegraphPoles;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

internal static class TelegraphPoleUtility
{
    private static TelegraphPoleManager? _Manager;
    public static  TelegraphPoleManager  Manager => _Manager ??= Object.FindAnyObjectByType<TelegraphPoleManager>()!;

    private static SimpleGraph.Runtime.SimpleGraph? _Graph;
    public static  SimpleGraph.Runtime.SimpleGraph  Graph => _Graph ??= Traverse.Create(_Manager!).Property<SimpleGraph.Runtime.SimpleGraph>("Graph")!.Value;

    public static TelegraphPole GetTelegraphPole(int nodeId) {
        Manager.TryGetPole(nodeId, out var pole);
        return pole!;
    }
}
