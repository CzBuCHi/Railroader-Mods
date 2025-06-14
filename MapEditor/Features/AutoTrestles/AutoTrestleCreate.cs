using MapEditor.MapState;
using Track;

namespace MapEditor.Features.AutoTrestles;

public sealed record AutoTrestleCreate(string Id, AutoTrestleData Data) : IStateStep
{
    public void Do() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        MapEditorPlugin.PatchEditor!.AddOrUpdateAutoTrestle(segment, _ => Data);
        AutoTrestleUtility.CreateTrestle(segment, Data);
    }

    public void Undo() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        MapEditorPlugin.PatchEditor!.RemoveAutoTrestle(segment);
    }

#if DEBUG
    public string DoText   => $"AutoTrestleCreate = {{ Id = {Id} }}";
    public string UndoText => $"AutoTrestleDestroy = {{ Id = {Id} }}";
#endif
}
