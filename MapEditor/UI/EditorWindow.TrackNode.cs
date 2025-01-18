using System;
using System.Linq;
using HarmonyLib;
using MapEditor.Behaviours;
using MapEditor.MapState;
using MapEditor.MapState.TrackNodeEditor;
using MapEditor.Visualizers;
using Track;
using UI.Builder;
using UnityEngine;

namespace MapEditor.UI;

public sealed partial class EditorWindow
{
    private TrackSegmentVisualizer[] _TrackNodeSegments = null!;

    private void BuildTrackNodeEditor(UIPanelBuilder builder, TrackNode trackNode) {
        builder.AddField("Id", builder.AddInputField(trackNode.id, _ => { })).Disable(true);
        builder.AddField("Position", builder.AddInputField(trackNode.transform.localPosition.ToString(), _ => { })).Disable(true);
        builder.AddField("Rotation", builder.AddInputField(trackNode.transform.localEulerAngles.ToString(), _ => { })).Disable(true);
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.Shared == null || MoveableObject.Shared.Mode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.Shared?.Mode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.Shared?.Mode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        if (Graph.Shared.IsSwitch(trackNode)) {
            builder.AddField("Flip Switch Stand",
                builder.AddToggle(() => trackNode.flipSwitchStand, val => MapStateEditor.NextStep(new TrackNodeUpdate(trackNode.id) { FlipSwitchStand = val }))!
            );
        }

        builder.AddSection("Operations", section => {
            section.ButtonStrip(strip => {
                strip.AddButton("Show", TrackNodeUtility.Show);
                strip.AddButton("Remove", TrackNodeUtility.Remove);
                strip.AddButton("Create new", TrackNodeUtility.Add).Disable(Graph.Shared.IsSwitch(trackNode));
                strip.AddButton("Split", TrackNodeUtility.Split).Disable(Graph.Shared.NodeIsDeadEnd(trackNode, out _));
            });
        });

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            MoveableObject.Create(trackNode.gameObject, mode, (_, _) =>  OnUpdate(trackNode), (startPosition, startRotation) =>  OnComplete(trackNode, startPosition, startRotation));
            _TrackNodeSegments = Graph.Shared.SegmentsConnectedTo(trackNode).Select(o => o.GetComponentInChildren<TrackSegmentVisualizer>(true)).ToArray();
            strip.Rebuild();
        };
    }

    private void OnUpdate(TrackNode trackNode) {
        _TrackNodeSegments.Do(o => o.PendingRebuild = true);
    }

    private void OnComplete(TrackNode trackNode, Vector3 startPosition, Quaternion startRotation) {
        MapStateEditor.NextStep(new TrackNodeUpdate(trackNode.id) {
            OriginalPosition = startPosition,
            OriginalRotation = startRotation,
            Position = trackNode.transform.localPosition,
            Rotation = trackNode.transform.localRotation
        });
    }
}
