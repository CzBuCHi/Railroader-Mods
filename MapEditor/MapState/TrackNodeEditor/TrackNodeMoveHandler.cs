using System.Linq;
using HarmonyLib;
using MapEditor.Behaviours;
using MapEditor.Visualizers;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor.MapState.TrackNodeEditor;

public sealed class TrackNodeMoveHandler : IMoveableObjectHandler
{
    private readonly TrackNode _TrackNode;

    public TrackNodeMoveHandler(TrackNode trackNode, MoveableObjectMode mode) {
        _TrackNode = trackNode;
        _Segments = Graph.Shared.SegmentsConnectedTo(trackNode).Select(o => o.GetComponentInChildren<TrackSegmentVisualizer>(true)).ToArray();
        Mode = mode;
    }

    public GameObject GameObject => _TrackNode.gameObject;

    public MoveableObjectMode Mode { get; }

    private readonly TrackSegmentVisualizer[] _Segments;
    public           Vector3                  StartPosition { get; private set; }
    public           Quaternion               StartRotation { get; private set; }

    public void OnStart() {
        StartPosition = _TrackNode.transform.localPosition;
        StartRotation = _TrackNode.transform.localRotation;
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        if (translation.HasValue) {
            _TrackNode.transform.localPosition = StartPosition + translation.Value;
        }

        if (rotation.HasValue) {
            _TrackNode.transform.localRotation = StartRotation * rotation.Value;
        }

        _Segments.Do(o => o.PendingRebuild = true);
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        return new TrackNodeUpdate(_TrackNode.id) {
            OriginalPosition = translation != null ? StartPosition : null,
            OriginalRotation = rotation != null ? StartRotation : null,
            Position = translation != null ? StartPosition + translation.Value : null,
            Rotation = rotation != null ? StartRotation * rotation.Value : null
        };
    }
}
