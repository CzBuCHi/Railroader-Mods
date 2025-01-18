using System.Reflection;
using MapEditor.Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using Vector3 = UnityEngine.Vector3;

namespace MapEditor.Extensions;

public static class PatchEditorExtensions
{
    public static void AddOrUpdateNode(this PatchEditor patchEditor, TrackNode trackNode) {
        patchEditor.AddOrUpdateNode(trackNode.id, trackNode.transform.localPosition, trackNode.transform.localEulerAngles, trackNode.flipSwitchStand);
    }

    public static void AddOrUpdateSegment(this PatchEditor patchEditor, TrackSegment trackSegment) {
        patchEditor.AddOrUpdateSegment(trackSegment.id, trackSegment.a.id, trackSegment.b.id, trackSegment.priority, trackSegment.groupId, trackSegment.speedLimit, trackSegment.style, trackSegment.trackClass);
    }

    public static void AddOrUpdateTelegraphPole(this PatchEditor patchEditor, int nodeId, Vector3 position, Vector3 rotation, int tag) {
        patchEditor.AddOrUpdateSpliney("TelegraphPoles", AddOrUpdate);
        return;

        JObject AddOrUpdate(JObject? o) {
            o ??= new() {
                { "handler", "MapMod.TelegraphPoleTransform" },
                { "nodes", new JObject() }
            };

            var nodes = (JObject)o["nodes"]!;
            nodes[$"{nodeId}"] = new JObject {
                { "position", new JArray { position.x, position.y, position.z } },
                { "rotation", new JArray { rotation.x, rotation.y, rotation.z } },
                { "tag", tag }
            };
            return o;
        }
    }

    public static void RemoveTelegraphPole(this PatchEditor patchEditor, int nodeId) {
        var splineys = patchEditor.GetSplineys();
        if (!splineys.TryGetValue("TelegraphPoles", out _)) {
            return;
        }

        patchEditor.AddOrUpdateSpliney("TelegraphPoles", AddOrUpdate);
        return;

        JObject AddOrUpdate(JObject? data) {
            var nodes = (JObject)data!["nodes"]!;
            nodes.Remove($"{nodeId}");
            return data;
        }
    }

    public static void AddOrUpdateScenery(this PatchEditor patchEditor, string id, SerializedScenery scenery) {
        var trackPatcherType = typeof(PatchEditor).Assembly.GetType("StrangeCustoms.Tracks.TrackPatcher");
        var serializer       = trackPatcherType.GetProperty("Serializer", BindingFlags.NonPublic | BindingFlags.Static)!;
        var jsonSerializer   = (JsonSerializer)serializer.GetValue(null!)!;

        var jObject = JObject.FromObject(scenery, jsonSerializer);

        patchEditor.ChangeThing("scenery", id, jObject, true);
    }


}
