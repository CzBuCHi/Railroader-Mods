using Track;
using UnityEngine;

namespace MapEditor.Features.TrackNodes;

public sealed record TrackNodeData(Vector3 Position, Vector3 EulerAngles, bool FlipSwitchStand = false, bool IsThrown = false)
{
    public TrackNodeData(TrackNode trackNode) : this(trackNode.transform.position, trackNode.transform.eulerAngles, trackNode.flipSwitchStand, trackNode.isThrown) {
    }
}
