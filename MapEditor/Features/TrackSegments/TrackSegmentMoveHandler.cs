using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MapEditor.Behaviours;
using MapEditor.Features.TrackNodes;
using MapEditor.MapState;
using Track;
using UnityEngine;

namespace MapEditor.Features.TrackSegments;

public sealed class TrackSegmentMoveHandler(TrackSegment trackSegment, MoveableObjectMode mode) : IMoveableObjectHandler
{
    private readonly HashSet<TrackSegment>    _Segments    = Graph.Shared.SegmentsAffectedByNodes([trackSegment.a, trackSegment.b])!;
    private          TrackSegmentVisualizer[] _Visualizers = null!;
    private          Vector3                  _StartPositionB;
    private          Quaternion               _StartRotationB;

    public Vector3 StartPosition { get; private set; }

    public Quaternion StartRotation { get; private set; }

    public GameObject         GameObject => trackSegment.gameObject;
    public MoveableObjectMode Mode       { get; } = mode;

    public void OnStart() {
        StartPosition = trackSegment.a.transform.localPosition;
        StartRotation = trackSegment.a.transform.localRotation;
        _StartPositionB = trackSegment.b.transform.localPosition;
        _StartRotationB = trackSegment.b.transform.localRotation;
        _Segments.Do(TrackSegmentVisualizer.ShowVisualizer);
        _Visualizers = _Segments.Select(o => o.GetComponentInChildren<TrackSegmentVisualizer>(true)).ToArray();
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        if (translation.HasValue) {
            trackSegment.a.transform.localPosition = StartPosition + translation.Value;
            trackSegment.b.transform.localPosition = _StartPositionB + translation.Value;
        }

        if (rotation.HasValue) {
            trackSegment.a.transform.localRotation = StartRotation * rotation.Value;
            trackSegment.b.transform.localRotation = _StartRotationB * rotation.Value;
        }

        _Visualizers.Do(o => o.PendingRebuild = true);
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        var updateA = new TrackNodeUpdate(trackSegment.a.id) {
            OriginalPosition = translation != null ? StartPosition : null,
            OriginalRotation = rotation != null ? StartRotation : null,
            Position = translation != null ? StartPosition + translation.Value : null,
            Rotation = rotation != null ? StartRotation * rotation.Value : null
        };
        var updateB = new TrackNodeUpdate(trackSegment.b.id) {
            OriginalPosition = translation != null ? _StartPositionB : null,
            OriginalRotation = rotation != null ? _StartRotationB : null,
            Position = translation != null ? _StartPositionB + translation.Value : null,
            Rotation = rotation != null ? _StartRotationB * rotation.Value : null
        };

        _Segments.Do(o => {
            if (o != trackSegment) {
                TrackSegmentVisualizer.HideVisualizer(o);
            }
        });

        return new CompoundSteps(updateA, updateB);
    }
}
