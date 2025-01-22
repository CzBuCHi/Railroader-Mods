using MapEditor.Utility;
using MapEditor.Visualizers;
using Track;

namespace MapEditor.MapState.TrackNodeEditor;

public sealed record TrackNodeDestroy(string Id) : IStateStep
{
    private TrackNodeData? _Data;

    public void Do() {
        var node = Graph.Shared.GetNode(Id);
        if (node == null) {
            return;
        }

        EditorState.RemoveFromSelection(node);
        _Data = TrackNodeUtility.Destroy(node);
    }

    public void Undo() {
        if (_Data == null) {
            return;
        }

        var node = TrackNodeUtility.Create(Id, _Data);
        UnityHelpers.CallOnNextFrame(() => TrackNodeVisualizer.CreateVisualizer(node));
    }

#if DEBUG
    public string DoText   => $"TrackNodeDestroy = {{ Id = {Id} }}";
    public string UndoText => $"TrackNodeCreate = {{ Id = {Id} }}";
#endif
}
