using System.Reflection;
using MapEditor.Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;

namespace MapEditor.Extensions;

public static class PatchEditorExtensions
{
    public static void AddOrUpdateScenery(this PatchEditor patchEditor, string id, SerializedScenery scenery) {
        var trackPatcherType = typeof(PatchEditor).Assembly.GetType("StrangeCustoms.Tracks.TrackPatcher");
        var serializer       = trackPatcherType.GetProperty("Serializer", BindingFlags.NonPublic | BindingFlags.Static)!;
        var jsonSerializer   = (JsonSerializer)serializer.GetValue(null!)!;

        var jObject = JObject.FromObject(scenery, jsonSerializer);

        patchEditor.ChangeThing("scenery", id, jObject, true);
    }
}
