using StrangeCustoms.Tracks;
using Track;

namespace MapEditor.Features.TrackNodes;

internal static class PatchEditorExtensions
{
    public static void AddOrUpdateNode(this PatchEditor patchEditor, TrackNode trackNode) =>
        patchEditor.AddOrUpdateNode(trackNode.id, trackNode.transform.localPosition, trackNode.transform.localEulerAngles, trackNode.flipSwitchStand);
}
