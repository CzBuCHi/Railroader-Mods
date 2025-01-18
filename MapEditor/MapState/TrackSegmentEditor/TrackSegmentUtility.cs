using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helpers;
using MapEditor.Extensions;
using MapEditor.MapState.AutoTrestleEditor;
using MapEditor.MapState.TrackNodeEditor;
using MapEditor.Utility;
using MapEditor.Visualizers;
using Serilog;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapEditor.MapState.TrackSegmentEditor;

internal static class TrackSegmentUtility
{
    public static void InjectNode() {
        // inject node in center of segment:
        // NODE_A  --- NODE_B
        // result:
        // NODE_A  --- NEW_NODE --- NODE_B
        var trackSegment = EditorState.TrackSegment!;

        var nodeA = trackSegment.a.id;
        var nodeB = trackSegment.b.id;

        var position    = trackSegment.Curve.GetPoint(0.5f).GameToWorld();
        var eulerAngles = trackSegment.Curve.GetRotation(0.5f).eulerAngles;

        EditorState.Update(state => state with { SelectedAsset = null });

        var nid = IdGenerators.TrackNodes.Next();
        var sid = IdGenerators.TrackSegments.Next();

        var actions = new List<IStateStep> {
            new TrackNodeCreate(nid, new TrackNodeData(position, eulerAngles)),
            new TrackSegmentUpdate(trackSegment.id) { A = nodeA, B = nid },
            new TrackSegmentCreate(sid, new TrackSegmentData(trackSegment) { StartId = nid, EndId = nodeB })
        };

        MapStateEditor.NextStep(new CompoundSteps(actions.ToArray()));
        UnityHelpers.CallOnNextFrame(() => {
            EditorState.Update(state => state with {
                SelectedAsset = Graph.Shared.GetNode(nid)
            });
        });
    }

    public static TrackSegmentData Destroy(TrackSegment trackSegment) {
        var trackSegmentData = new TrackSegmentData(trackSegment);
        Object.Destroy(trackSegment.gameObject);
        MapEditorPlugin.PatchEditor!.RemoveSegment(trackSegment.id);
        return trackSegmentData;
    }

    public static void Create(string id, TrackSegmentData trackSegmentData) {
        var a = Graph.Shared.GetNode(trackSegmentData.StartId);
        var b = Graph.Shared.GetNode(trackSegmentData.EndId);
        if (a == null || b == null) {
            return;
        }

        var gameObject = new GameObject(id);
        gameObject.SetActive(false);
        gameObject.transform.parent = Graph.Shared.transform;
        var trackSegment = gameObject.AddComponent<TrackSegment>();
        trackSegment.id = id;
        trackSegment.transform.position = trackSegmentData.Position;
        trackSegment.transform.eulerAngles = trackSegmentData.EulerAngles;
        gameObject.SetActive(true);

        trackSegment.a = a;
        trackSegment.b = b;
        trackSegment.style = trackSegmentData.Style;
        trackSegment.trackClass = trackSegmentData.TrackClass;
        trackSegment.priority = trackSegmentData.Priority;
        trackSegment.speedLimit = trackSegmentData.SpeedLimit;
        trackSegment.groupId = trackSegmentData.GroupId!;
        trackSegment.InvalidateCurve();

        Graph.Shared.AddSegment(trackSegment);
        MapEditorPlugin.PatchEditor!.AddOrUpdateSegment(trackSegment);
    }

    public static void UpdateSegment(string? groupId, int? priority, int? speedLimit, TrackClass? trackClass, TrackSegment.Style? style, bool? trestle, AutoTrestle.AutoTrestle.EndStyle trestleHead, AutoTrestle.AutoTrestle.EndStyle trestleTail) {
        var segment = EditorState.TrackSegment;
        IStateStep step = new TrackSegmentUpdate(segment!.id) {
            GroupId = groupId,
            Priority = priority,
            SpeedLimit = speedLimit,
            TrackClass = trackClass,
            Style = style
        };

        var oldStyle = segment.style;

        if (style != null && style != oldStyle) {
            Log.Information("Style changed: " + oldStyle + " -> " + style);

            if (style == TrackSegment.Style.Bridge && trestle == true) {
                Log.Information("Bridge w trestle -> AutoTrestleCreate");

                var autoTrestleCreate = new AutoTrestleCreate(segment.id, AutoTrestleUtility.CreateAutoTrestleData(segment, trestleHead, trestleTail));
                step = new CompoundSteps(step, autoTrestleCreate);
            } else {
                Log.Information("not bridge -> AutoTrestleDestroy");
                var autoTrestleDestroy = new AutoTrestleDestroy(segment.id);
                step = new CompoundSteps(step, autoTrestleDestroy);
            }
        } else if (oldStyle == TrackSegment.Style.Bridge) {
            Log.Information("Same Bridge: AutoTrestleUpdate");
            var autoTrestleUpdate = new AutoTrestleUpdate(segment.id) {
                HeadStyle = trestleHead,
                TailStyle = trestleTail
            };
            step = new CompoundSteps(step, autoTrestleUpdate);
        }

        MapStateEditor.NextStep(step);
        UnityHelpers.CallOnNextFrame(TrackObjectManager.Instance.Rebuild);
    }

    public static void Remove() {
        var trackSegment = EditorState.TrackSegment!;
        EditorState.Update(state => state with { SelectedAsset = null });
        MapStateEditor.NextStep(new TrackSegmentDestroy(trackSegment.id));
    }

    public static void MoveByA(Vector3 startPosition, Quaternion startRotation) {
        var trackSegment     = EditorState.TrackSegment!;
        var deltaRotation    = trackSegment.a.transform.localRotation * Quaternion.Inverse(startRotation);
        var relativePosition = trackSegment.b.transform.localPosition - startPosition;
        var newPosition      = deltaRotation * relativePosition + trackSegment.a.transform.localPosition;
        var newRotation      = deltaRotation * trackSegment.b.transform.localRotation;

        trackSegment.b.transform.localPosition = newPosition;
        trackSegment.b.transform.localRotation = newRotation;
    }

    public static void Straighten() {
        var trackSegment = EditorState.TrackSegment!;

        var forwardDirection = trackSegment.a.transform.forward.normalized;

        var positionA = trackSegment.a.transform.localPosition;
        var distance     = (trackSegment.b.transform.localPosition - positionA).magnitude;
        var newPositionB = positionA + forwardDirection * distance;

        MapStateEditor.NextStep(new TrackNodeUpdate(trackSegment.b.id) {
            OriginalPosition = trackSegment.b.transform.localPosition,
            OriginalRotation = trackSegment.b.transform.localRotation,
            Position = newPositionB,
            Rotation = trackSegment.a.transform.localRotation
        });
    }
}



