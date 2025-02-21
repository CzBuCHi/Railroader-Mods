﻿using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using MapEditor.Behaviours;
using MapEditor.Extensions;
using MapEditor.MapState.TrackSegmentEditor;
using MapEditor.Utility;
using Serilog;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapEditor.MapState.TrackNodeEditor;

public static class TrackNodeUtility
{
    public static void Show(TrackNode trackNode) => CameraSelector.shared.ZoomToPoint(trackNode.transform.localPosition);

    public static void Remove(TrackNode trackNode) {
        MoveableObject.Destroy();

        var connectSegments = InputHelper.GetShift();
        Log.Information($"Remove node {trackNode.id}; connectSegments = {connectSegments}");

        EditorState.RemoveFromSelection(trackNode);

        var steps = Graph.Shared.DecodeSwitchAt(trackNode, out var enter, out var segmentA, out var segmentB)
            ? RemoveSwitch(connectSegments, trackNode, enter!, segmentA!, segmentB!)
            : RemoveSimple(connectSegments, trackNode);

        MapStateEditor.NextStep(new CompoundSteps(steps.ToArray()));
        UnityHelpers.CallOnNextFrame(TrackObjectManager.Instance.Rebuild);
    }

    private static IEnumerable<IStateStep> RemoveSimple(bool connectSegments, TrackNode node) {
        // end track node remove:
        // NODE_A --- NODE
        // result:
        // NODE_A

        // simple track node remove:
        // NODE_A -1- NODE -2- NODE_B
        // result:
        // NODE_A     NODE_B
        // result (connectSegments):
        // NODE_A -1- NODE_B
        var segments = Graph.Shared.SegmentsConnectedTo(node);
        if (connectSegments) {
            var firstSegment = segments.First();
            var lastSegment  = segments.Last();

            var a = firstSegment.GetOtherNode(node)!.id;
            var b = lastSegment.GetOtherNode(node)!.id;
            if (firstSegment.a.id == a) {
                yield return new TrackSegmentUpdate(firstSegment.id) { B = b };
            } else {
                yield return new TrackSegmentUpdate(firstSegment.id) { A = b };
            }

            yield return new TrackSegmentDestroy(lastSegment.id);
        } else {
            foreach (var segment in segments) {
                yield return new TrackSegmentDestroy(segment.id);
            }
        }

        yield return new TrackNodeDestroy(node.id);
    }

    private static IEnumerable<IStateStep> RemoveSwitch(bool connectSegments, TrackNode node, TrackSegment segmentEnter, TrackSegment segmentA, TrackSegment segmentB) {
        // switch track node remove:
        // NODE_A -1-\
        //            >- NODE -3- NODE_C
        // NODE_B -2-/
        // result:
        // NODE_A
        //               NODE_C
        // NODE_B
        // result (connectSegments):
        // NODE_A -1-\
        //            >- NODE_C
        // NODE_B -2-/

        yield return new TrackSegmentDestroy(segmentEnter.id);
        if (connectSegments) {
            var a     = segmentA.GetOtherNode(node)!.id;
            var b     = segmentB.GetOtherNode(node)!.id;
            var enter = segmentEnter.GetOtherNode(node)!.id;

            if (segmentA.a.id == a) {
                yield return new TrackSegmentUpdate(segmentA.id) { B = enter };
            } else {
                yield return new TrackSegmentUpdate(segmentA.id) { A = enter };
            }

            if (segmentB.a.id == b) {
                yield return new TrackSegmentUpdate(segmentB.id) { B = enter };
            } else {
                yield return new TrackSegmentUpdate(segmentB.id) { A = enter };
            }
        } else {
            yield return new TrackSegmentDestroy(segmentA.id);
            yield return new TrackSegmentDestroy(segmentB.id);
        }

        yield return new TrackNodeDestroy(node.id);
    }

    public static void Add(TrackNode trackNode) {
        MoveableObject.Destroy();

        var withoutSegment = InputHelper.GetShift();

        if (!Graph.Shared.NodeIsDeadEnd(trackNode, out var direction)) {
            direction = Vector3.Cross(trackNode.transform.forward, Vector3.up);
        }

        var        nid  = IdGenerators.TrackNodes.Next();
        IStateStep step = new TrackNodeCreate(nid, new TrackNodeData(trackNode.transform.position + direction * 2.5f, trackNode.transform.eulerAngles));

        if (!withoutSegment) {
            var sid           = IdGenerators.TrackSegments.Next();
            var createSegment = new TrackSegmentCreate(sid, new TrackSegmentData(Vector3.zero, Vector3.zero, nid, trackNode.id));
            step = new CompoundSteps(step, createSegment);
        }

        MapStateEditor.NextStep(step);
        var newNode = Graph.Shared.GetNode(nid)!;
        EditorState.ReplaceSelection(newNode);
        UnityHelpers.CallOnNextFrame(() => TrackObjectManager.Instance.Rebuild());
    }

    public static void Split(TrackNode trackNode) {
        // simple track node split:
        // NODE_A --- NODE --- NODE_B
        // result:
        // NODE_A --- NODE
        //            NEW_NODE --- NODE_B

        // switch node split:
        // NODE_A ---\
        //            >- NODE --- NODE_C
        // NODE_B ---/
        // result:
        // NODE_A --- NODE
        // NODE_B --- NEW_NODE_1
        //            NEW_NODE_2 --- NODE_C

        MoveableObject.Destroy();
        
        var segments = Graph.Shared.SegmentsConnectedTo(trackNode).ToList();

        List<IStateStep> actions = new();

        if (Graph.Shared.DecodeSwitchAt(trackNode, out _, out var segmentA, out var segmentB)) {
            var left = Vector3.Cross(trackNode.transform.forward, Vector3.up) * 0.75f;

            var a = trackNode.transform.position;
            var b = segmentA!.GetOtherNode(trackNode)!.transform.position;
            var c = segmentB!.GetOtherNode(trackNode)!.transform.position;

            if (Intersect(b, a + left, c, a - left)) {
                left = -left;
            }

            UpdateSegment(segmentA, true, left);
            UpdateSegment(segmentB, true, -left);
        } else {
            UpdateSegment(segments[1]!, false, Vector3.zero);
        }

        MapStateEditor.NextStep(new CompoundSteps(actions.ToArray()));
        return;

        void UpdateSegment(TrackSegment trackSegment, bool isSwitch, Vector3 offset) {
            var endIsA = trackSegment.EndForNode(trackNode) == TrackSegment.End.A;

            var nid = IdGenerators.TrackNodes.Next();

            var par   = Math.Min(trackSegment.GetLength(), 5);
            trackSegment.GetPositionRotationAtDistance(par, endIsA ? TrackSegment.End.A : TrackSegment.End.B, PositionAccuracy.Standard, out var position, out _);
            
            var point = position.GameToWorld();

            var forward = trackNode.transform.forward * (isSwitch ? 2 : 5);
            if (Vector3.Angle(trackNode.transform.forward, point - trackNode.transform.position) > Math.PI) {
                forward = -forward;
            }

            actions.Add(new TrackNodeCreate(nid, new TrackNodeData(trackNode.transform.position + forward + offset, trackNode.transform.eulerAngles)));

            var a = endIsA ? trackSegment.b.id : nid;
            var b = endIsA ? nid : trackSegment.a.id;

            actions.Add(new TrackSegmentUpdate(trackSegment.id) { A = a, B = b });
        }

        bool Intersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            var denominator = (b.x - a.x) * (d.z - c.z) - (b.z - a.z) * (d.x - c.x);

            if (Mathf.Abs(denominator) < Mathf.Epsilon) {
                return false;
            }

            var numerator1 = (a.z - c.z) * (d.x - c.x) - (a.x - c.x) * (d.z - c.z);
            var numerator2 = (a.z - c.z) * (b.x - a.x) - (a.x - c.x) * (b.z - a.z);

            var t1 = numerator1 / denominator;
            var t2 = numerator2 / denominator;

            return t1 is >= 0 and <= 1 && t2 is >= 0 and <= 1;
        }
    }

    public static TrackNodeData Destroy(TrackNode trackNode) {
        MoveableObject.Destroy();

        var trackNodeData = new TrackNodeData(trackNode);
        Object.Destroy(trackNode.gameObject);
        MapEditorPlugin.PatchEditor!.RemoveNode(trackNode.id);
        return trackNodeData;
    }

    public static TrackNode Create(string id, TrackNodeData trackNodeData) {
        MoveableObject.Destroy();

        var gameObject = new GameObject(id);
        gameObject.SetActive(false);
        gameObject.transform.parent = Graph.Shared.transform;
        var trackNode = gameObject.AddComponent<TrackNode>();

        trackNode.id = id;
        trackNode.transform.position = trackNodeData.Position;
        trackNode.transform.eulerAngles = trackNodeData.EulerAngles;
        gameObject.SetActive(true);

        trackNode.flipSwitchStand = trackNodeData.FlipSwitchStand;
        trackNode.isThrown = trackNodeData.IsThrown;

        Graph.Shared.AddNode(trackNode);
        MapEditorPlugin.PatchEditor!.AddOrUpdateNode(trackNode);
        return trackNode;
    }

    private static Quaternion? _SavedRotation;

    public static void CopyRotation(TrackNode trackNode) {
        MoveableObject.Destroy();
        _SavedRotation = trackNode.transform.localRotation;
    }

    public static void SetRotation(TrackNode trackNode) {
        if (_SavedRotation == null) {
            return;
        }

        MoveableObject.Destroy();
        MapStateEditor.NextStep(new TrackNodeUpdate(trackNode.id) {
            OriginalRotation = trackNode.transform.localRotation,
            Rotation = _SavedRotation.Value
        });
    }
}
