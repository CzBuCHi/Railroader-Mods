using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms;
using TelegraphPoles;
using UnityEngine;

namespace MapMod.TelegraphPoles;

[PublicAPI]
public sealed class TelegraphPoleBuilder : ISplineyBuilder
{
    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data) {
        var poles = Utility.Deserialize<TelegraphPoles>(data);
        var manager = Object.FindObjectOfType<TelegraphPoleManager>();
        var graph   = Traverse.Create(manager!).Property<SimpleGraph.Runtime.SimpleGraph>("Graph")!.Value;

        foreach (var pair in poles.Nodes) {
            var node   = graph.NodeForId(pair.Key)!;
            if (node == null!) {
                Log.Warning($"Cannot find telegraph pole with id '{pair.Key}'.");
                continue;
            }

            node.position = pair.Value!.Position;
            node.eulerAngles = pair.Value.Rotation;
            node.tag = pair.Value.Tag;
        }

        return new GameObject();
    }
}