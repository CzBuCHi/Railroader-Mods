using MapEditor.Extensions;
using MapEditor.MapState;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

public sealed record TelegraphPoleUpdate(int Id) : IStateStep
{
    public Vector3? OriginalPosition { get; init; }
    public Vector3? OriginalRotation { get; init; }

    public Vector3? Position { get; init; }
    public Vector3? Rotation { get; init; }

    public void Do() {
        if (Position == null && Rotation == null) {
            return;
        }

        var node = TelegraphPoleUtility.Graph.NodeForId(Id)!;
        if (node == null!) {
            return;
        }

        if (Position != null) {
            node.position = Position.Value.Clone();
        }

        if (Rotation != null) {
            node.eulerAngles = Rotation.Value.Clone();
        }

        TelegraphPoleUtility.Manager.Rebuild();
        MapEditorPlugin.PatchEditor!.AddOrUpdateTelegraphPole(Id, node.position, node.eulerAngles, node.tag);
    }

    public void Undo() {
        if (OriginalPosition == null && OriginalRotation == null) {
            return;
        }

        var node = TelegraphPoleUtility.Graph.NodeForId(Id)!;
        if (node == null!) {
            return;
        }

        if (OriginalPosition != null) {
            node.position = OriginalPosition.Value;
        }

        if (OriginalRotation != null) {
            node.eulerAngles = OriginalRotation.Value;
        }

        TelegraphPoleUtility.Manager.Rebuild();
        MapEditorPlugin.PatchEditor!.AddOrUpdateTelegraphPole(Id, node.position, node.eulerAngles, node.tag);
    }

#if DEBUG
    public string DoText {
        get {
            var node = TelegraphPoleUtility.Graph.NodeForId(Id)!;
            return "TelegraphPoleUpdate { Id = " + Id + ", " +
                   (Position != null ? $"Position = {node.position} -> {Position}, " : "") +
                   (Rotation != null ? $"Rotation = {node.eulerAngles} -> {Rotation}, " : "") +
                   " }";
        }
    }

    public string UndoText {
        get {
            var node = TelegraphPoleUtility.Graph.NodeForId(Id)!;
            return "TelegraphPoleUpdate { Id = " + Id + ", " +
                   (OriginalPosition != null ? $"Position = {node.position} -> {OriginalPosition}, " : "") +
                   (OriginalRotation != null ? $"Rotation = {node.eulerAngles} -> {OriginalRotation}, " : "") +
                   " }";
        }
    }
#endif
}
