using MapEditor.MapState;
using Track;

namespace MapEditor.Features.AutoTrestles;

public sealed record AutoTrestleUpdate(string Id) : IStateStep
{
    private AutoTrestle.AutoTrestle.EndStyle? _HeadStyle;
    private AutoTrestle.AutoTrestle.EndStyle? _TailStyle;

    public AutoTrestle.AutoTrestle.EndStyle? HeadStyle { get; init; }
    public AutoTrestle.AutoTrestle.EndStyle? TailStyle { get; init; }

    public void Do() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        var data = MapEditorPlugin.PatchEditor!.GetAutoTrestle(segment)!;
        _HeadStyle = data.HeadStyle;
        _TailStyle = data.TailStyle;

        MapEditorPlugin.PatchEditor!.AddOrUpdateAutoTrestle(segment, AutoTrestleUtility.CreateOrUpdate(segment, HeadStyle, TailStyle));
        AutoTrestleUtility.UpdateTrestle(segment);
    }

    public void Undo() {
        var segment = Graph.Shared.GetSegment(Id);
        if (segment == null) {
            return;
        }

        MapEditorPlugin.PatchEditor!.AddOrUpdateAutoTrestle(segment, AutoTrestleUtility.CreateOrUpdate(segment, _HeadStyle, _TailStyle));
        AutoTrestleUtility.UpdateTrestle(segment);
    }

#if DEBUG
    public string DoText {
        get {
            var autoTrestleData = MapEditorPlugin.PatchEditor!.GetAutoTrestle(Graph.Shared.GetSegment(Id)!)!;
            return "AutoTrestleUpdate { " +
                   (HeadStyle != null ? $"HeadStyle = {autoTrestleData.HeadStyle} -> {HeadStyle}, " : "") +
                   (TailStyle != null ? $"TailStyle = {autoTrestleData.TailStyle} -> {TailStyle}, " : "") +
                   " }";
        }
    }

    public string UndoText {
        get {
            var autoTrestleData = MapEditorPlugin.PatchEditor!.GetAutoTrestle(Graph.Shared.GetSegment(Id)!)!;
            return "AutoTrestleUpdate { " +
                   (_HeadStyle != null ? $"HeadStyle = {autoTrestleData.HeadStyle} -> {_HeadStyle}, " : "") +
                   (_TailStyle != null ? $"TailStyle = {autoTrestleData.TailStyle} -> {_TailStyle}, " : "") +
                   " }";
        }
    }
#endif
}
