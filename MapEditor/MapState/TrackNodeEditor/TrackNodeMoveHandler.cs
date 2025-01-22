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
    private          Vector3                  _StartPosition;
    private          Quaternion               _StartRotation;

    public void OnStart() {
        _StartPosition = _TrackNode.transform.localPosition;
        _StartRotation = _TrackNode.transform.localRotation;
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        if (translation.HasValue) {
            _TrackNode.transform.localPosition = _StartPosition + translation.Value;
        }

        if (rotation.HasValue) {
            _TrackNode.transform.localRotation = _StartRotation * rotation.Value;
        }

        _Segments.Do(o => o.PendingRebuild = true);
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        return new TrackNodeUpdate(_TrackNode.id) {
            OriginalPosition = translation != null ? _StartPosition : null,
            OriginalRotation = rotation != null ? _StartRotation : null,
            Position = translation != null ? _StartPosition + translation.Value : null,
            Rotation = rotation != null ? _StartRotation * rotation.Value : null
        };
    }
}
