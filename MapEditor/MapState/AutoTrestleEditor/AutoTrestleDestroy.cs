using MapEditor.MapState.AutoTrestleEditor.StrangeCustoms;
using Track;

namespace MapEditor.MapState.AutoTrestleEditor;

public sealed record AutoTrestleDestroy(string Id) : IStateStep
{
    private AutoTrestleData? _Data;

    public void Do() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        _Data = MapEditorPlugin.PatchEditor!.GetAutoTrestle(segment)!;
        MapEditorPlugin.PatchEditor!.RemoveAutoTrestle(segment);
        AutoTrestleUtility.RemoveTrestle(segment);
    }

    public void Undo() {
        if (_Data == null) {
            return;
        }

        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        MapEditorPlugin.PatchEditor!.AddOrUpdateAutoTrestle(segment, _ => _Data);
        AutoTrestleUtility.CreateTrestle(segment, _Data);
    }

#if DEBUG
    public string DoText   => $"AutoTrestleDestroy = {{ Id = {Id} }}";
    public string UndoText => $"AutoTrestleCreate = {{ Id = {Id} }}";
#endif
}
