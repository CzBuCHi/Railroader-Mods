using Track;

namespace MapEditor.MapState.TrackSegmentEditor;

public sealed record TrackSegmentDestroy(string Id) : IStateStep
{
    private TrackSegmentData? _Data;

    public void Do() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        EditorState.RemoveFromSelection(segment);
        _Data = TrackSegmentUtility.Destroy(segment);
    }

    public void Undo() {
        if (_Data == null) {
            return;
        }

        TrackSegmentUtility.Create(Id, _Data);
    }

#if DEBUG
    public string DoText {
        get {
            var segment = Graph.Shared.GetSegment(Id)!;
            return $"TrackSegmentDestroy = {{ Id = {Id}, StartId = {segment.a.id}, EndId = {segment.b.id}  }}";
        }
    }

    public string UndoText => $"TrackSegmentCreate = {{ Id = {Id}, StartId = {_Data!.StartId}, EndId = {_Data.EndId} }}";
#endif
}
