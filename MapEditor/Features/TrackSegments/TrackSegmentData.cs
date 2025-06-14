using Track;
using UnityEngine;

namespace MapEditor.Features.TrackSegments;

public sealed record TrackSegmentData(Vector3 Position, Vector3 EulerAngles, string StartId, string EndId)
{
    public TrackSegmentData(TrackSegment trackSegment) : this(trackSegment.transform.position, trackSegment.transform.eulerAngles, trackSegment.a.id, trackSegment.b.id) {
        Style = trackSegment.style;
        TrackClass = trackSegment.trackClass;
        Priority = trackSegment.priority;
        SpeedLimit = trackSegment.speedLimit;
        GroupId = trackSegment.groupId;
    }

    public TrackSegment.Style Style      { get; init; }
    public TrackClass         TrackClass { get; init; }
    public int                Priority   { get; init; }
    public int                SpeedLimit { get; init; }
    public string?            GroupId    { get; init; }
}
