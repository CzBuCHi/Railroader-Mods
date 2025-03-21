﻿using MapEditor.Utility;
using MapEditor.Visualizers;
using Track;

namespace MapEditor.MapState.TrackNodeEditor;

public sealed record TrackNodeCreate(string Id, TrackNodeData Data) : IStateStep
{
    public void Do() {
        var node = TrackNodeUtility.Create(Id, Data);
        EditorState.ReplaceSelection(node);
        UnityHelpers.CallOnNextFrame(() => TrackNodeVisualizer.CreateVisualizer(node));
    }

    public void Undo() {
        var trackNode = Graph.Shared.GetNode(Id);
        if (trackNode == null) {
            return;
        }

        EditorState.RemoveFromSelection(trackNode);
        TrackNodeUtility.Destroy(trackNode);
    }

#if DEBUG
    public string DoText   => $"TrackNodeCreate = {{ Id = {Id} }}";
    public string UndoText => $"TrackNodeDestroy = {{ Id = {Id} }}";
#endif
}
