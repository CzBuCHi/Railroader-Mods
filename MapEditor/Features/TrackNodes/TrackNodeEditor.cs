using System;
using MapEditor.Behaviours;
using MapEditor.MapState;
using Track;
using UI.Builder;

namespace MapEditor.Features.TrackNodes;

public static class TrackNodeEditor
{
    public static void Build(UIPanelBuilder builder, TrackNode trackNode) {
        builder.AddField("Id", builder.AddInputField(trackNode.id, _ => { }));
        builder.AddField("Position", builder.AddInputField(trackNode.transform.localPosition.ToString(), _ => { }));
        builder.HStack(stack =>
            stack.AddField("Rotation", stack.HStack(field => {
                field.AddInputField(trackNode.transform.localEulerAngles.ToString(), _ => { }).FlexibleWidth();
                field.AddButtonCompact("Copy", () => TrackNodeUtility.CopyRotation(trackNode));
                field.AddButtonCompact("Set", () => TrackNodeUtility.SetRotation(trackNode));
            }))
        );
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.ActiveMode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.ActiveMode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.ActiveMode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        if (Graph.Shared.IsSwitch(trackNode)) {
            builder.AddField("Flip Switch Stand",
                builder.AddToggle(() => trackNode.flipSwitchStand, val => MapStateEditor.NextStep(new TrackNodeUpdate(trackNode.id) { FlipSwitchStand = val }))!
            );
        }

        builder.AddSection("Operations", section => {
            section.ButtonStrip(strip => {
                strip.AddButton("Show", () => TrackNodeUtility.Show(trackNode));
                strip.AddButton("Remove", () => TrackNodeUtility.Remove(trackNode));
                strip.AddButton("Create new", () => TrackNodeUtility.Add(trackNode)).Disable(Graph.Shared.IsSwitch(trackNode));
                strip.AddButton("Split", () => TrackNodeUtility.Split(trackNode)).Disable(Graph.Shared.NodeIsDeadEnd(trackNode, out _));
            });
        });

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            MoveableObject.Destroy();
            if (mode != MoveableObjectMode.None) {
                MoveableObject.Create(new TrackNodeMoveHandler(trackNode, mode));
            }

            strip.Rebuild();
        };
    }
}
