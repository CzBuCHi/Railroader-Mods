using MapEditor.MapState;
using Track;

namespace MapEditor.Features.TrackSegments;

public sealed record TrackSegmentCreate(string Id, TrackSegmentData Data) : IStateStep
{
    public void Do() {
        TrackSegmentUtility.Create(Id, Data);
    }

    public void Undo() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        TrackSegmentUtility.Destroy(segment);
    }

#if DEBUG
    public string DoText   => $"TrackSegmentCreate = {{ Id = {Id}, StartId = {Data.StartId}, EndId = {Data.EndId} }}";
    public string UndoText => $"TrackSegmentDestroy = {{ Id = {Id}, StartId = {Data.StartId}, EndId = {Data.EndId}  }}";
#endif
}
