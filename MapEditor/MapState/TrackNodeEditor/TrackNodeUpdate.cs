using System.Linq;
using HarmonyLib;
using MapEditor.Extensions;
using MapEditor.Visualizers;
using Track;
using UnityEngine;

namespace MapEditor.MapState.TrackNodeEditor;

public sealed record TrackNodeUpdate(string Id) : IStateStep
{
    public Vector3?    OriginalPosition        { get; init; }
    public Quaternion? OriginalRotation        { get; init; }
    public bool?       OriginalFlipSwitchStand { get; init; }

    public Vector3?    Position        { get; init; }
    public Quaternion? Rotation        { get; init; }
    public bool?       FlipSwitchStand { get; init; }

    public void Do() {
        if (Position == null && Rotation == null && FlipSwitchStand == null) {
            return;
        }

        var node = Graph.Shared.GetNode(Id);
        if (node == null) {
            return;
        }

        if (Position != null) {
            node.transform.localPosition = Position.Value.Clone();
        }

        if (Rotation != null) {
            node.transform.localRotation = Rotation.Value.Clone();
        }

        if (FlipSwitchStand != null) {
            node.flipSwitchStand = FlipSwitchStand.Value;
        }

        Graph.Shared.OnNodeDidChange(node);
        Graph.Shared.SegmentsConnectedTo(node).Select(o => o.GetComponentInChildren<TrackSegmentVisualizer>(true)).Do(o => o.PendingRebuild = true);
        MapEditorPlugin.PatchEditor!.AddOrUpdateNode(node);
    }

    public void Undo() {
        if (OriginalPosition == null && OriginalRotation == null && OriginalFlipSwitchStand == null) {
            return;
        }

        var node = Graph.Shared.GetNode(Id);
        if (node == null) {
            return;
        }

        if (OriginalPosition != null) {
            node.transform.localPosition = OriginalPosition.Value;
        }

        if (OriginalRotation != null) {
            node.transform.localRotation = OriginalRotation.Value;
        }

        if (OriginalFlipSwitchStand != null) {
            node.flipSwitchStand = OriginalFlipSwitchStand.Value;
        }

        Graph.Shared.OnNodeDidChange(node);
        Graph.Shared.SegmentsConnectedTo(node).Select(o => o.GetComponentInChildren<TrackSegmentVisualizer>(true)).Do(o => o.PendingRebuild = true);
        MapEditorPlugin.PatchEditor!.AddOrUpdateNode(node);
    }

#if DEBUG
    public string DoText {
        get {
            var node = Graph.Shared.GetNode(Id)!;
            return "TrackNodeUpdate { Id = " + Id + ", " +
                   (Position != null ? $"Position = {node.transform.localPosition} -> {Position}, " : "") +
                   (Rotation != null ? $"Rotation = {node.transform.eulerAngles} -> {Rotation}, " : "") +
                   (FlipSwitchStand != null ? $"FlipSwitchStand = {node.flipSwitchStand} -> {FlipSwitchStand}, " : "") +
                   " }";
        }
    }

    public string UndoText {
        get {
            var node = Graph.Shared.GetNode(Id)!;
            return "TrackNodeUpdate { Id = " + Id + ", " +
                   (OriginalPosition != null ? $"Position = {node.transform.localPosition} -> {OriginalPosition}, " : "") +
                   (OriginalRotation != null ? $"Rotation = {node.transform.eulerAngles} -> {OriginalRotation}, " : "") +
                   (OriginalFlipSwitchStand != null ? $"FlipSwitchStand = {node.flipSwitchStand} -> {OriginalFlipSwitchStand}, " : "") +
                   " }";
        }
    }
#endif
}
