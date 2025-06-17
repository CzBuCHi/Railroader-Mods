using StrangeCustoms.Tracks;
using Track;

namespace MapEditor.Features.TrackSegments;

internal static class PatchEditorExtensions
{
    public static void AddOrUpdateSegment(this PatchEditor patchEditor, TrackSegment trackSegment) =>
        patchEditor.AddOrUpdateSegment(trackSegment.id, trackSegment.a.id, trackSegment.b.id, trackSegment.priority, trackSegment.groupId, trackSegment.speedLimit, trackSegment.style, trackSegment.trackClass);
}
