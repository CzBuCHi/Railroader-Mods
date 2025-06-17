using MapMod.TelegraphPoles;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

internal static class PatchEditorExtensions
{
    public static void AddOrUpdateTelegraphPole(this PatchEditor patchEditor, int nodeId, Vector3 position, Vector3 rotation, int tag) {
        patchEditor.AddOrUpdateSpliney("TelegraphPoles", AddOrUpdate);
        return;

        JObject AddOrUpdate(JObject? data) {
            var poles = data != null
                ? MapMod.Utility.Deserialize<MapMod.TelegraphPoles.TelegraphPoles>(data)
                : new MapMod.TelegraphPoles.TelegraphPoles();

            poles.Nodes[nodeId] = new TelegraphPoleData {
                Position = position,
                Rotation = rotation,
                Tag = tag
            };

            return MapMod.Utility.Serialize(poles);
        }
    }
}
