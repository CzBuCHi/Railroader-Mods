using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using StrangeCustoms;
using TelegraphPoles;
using UnityEngine;

namespace MapMod;

[PublicAPI]
public sealed class TelegraphPoleTransform : ISplineyBuilder
{
    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data) {
        var manager = Object.FindObjectOfType<TelegraphPoleManager>();
        var graph   = Traverse.Create(manager!).Property<SimpleGraph.Runtime.SimpleGraph>("Graph")!.Value;

        var nodes = (JObject)data["nodes"]!;
        foreach (var property in nodes.Properties()) {
            var jNode  = (JObject)property.Value;
            var nodeId = int.Parse(property.Name);
            var node   = graph.NodeForId(nodeId)!;

            node.position = FromArray((JArray)jNode["position"]!);
            node.eulerAngles = FromArray((JArray)jNode["rotation"]!);
            node.tag = jNode["tag"]!.Value<int>();
        }

        return new GameObject();

        Vector3 FromArray(JArray array) =>
            new(
                array[0].Value<float>(),
                array[1].Value<float>(),
                array[2].Value<float>()
            );
    }
}
