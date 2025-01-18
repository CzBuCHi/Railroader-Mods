using System;
using MapEditor.MapState.AutoTrestleEditor.StrangeCustoms;
using StrangeCustoms;
using Track;
using UnityEngine;

namespace MapEditor.MapState.AutoTrestleEditor;

public static class AutoTrestleUtility
{
    private static string GetTrestleId(TrackSegment trackSegment) => trackSegment.id + "_Trestle";

    public static void CreateTrestle(TrackSegment segment, AutoTrestleData data) {
        var builder = new AutoTrestleBuilder();
        builder.BuildAutoTrestle(GetTrestleId(segment), segment.a.transform.parent, data);
    }

    public static void UpdateTrestle(TrackSegment segment) {
        var trestleId = GetTrestleId(segment);

        var parentTransform = segment.a.transform.parent;

        var data = MapEditorPlugin.PatchEditor!.GetAutoTrestle(segment)!;

        var builder = new AutoTrestleBuilder();

        builder.RemoveTrestle(trestleId, parentTransform);
        builder.BuildAutoTrestle(trestleId, parentTransform, data);
    }

    public static void RemoveTrestle(TrackSegment segment) {
        var builder = new AutoTrestleBuilder();
        builder.RemoveTrestle(GetTrestleId(segment), segment.a.transform.parent);
    }

    private static readonly Vector3 _TrestleOffset = new(0, -0.35f, 0);

    public static Func<AutoTrestleData?, AutoTrestleData> CreateOrUpdate(TrackSegment trackSegment, AutoTrestle.AutoTrestle.EndStyle? headStyle, AutoTrestle.AutoTrestle.EndStyle? tailStyle) {
        return data => {
            data ??= new AutoTrestleData();

            data.Points = [
                new SerializedSplinePoint { Position = trackSegment.a.transform.localPosition + _TrestleOffset, Rotation = trackSegment.a.transform.eulerAngles },
                new SerializedSplinePoint { Position = trackSegment.b.transform.localPosition + _TrestleOffset, Rotation = trackSegment.b.transform.eulerAngles }
            ];

            if (headStyle != null) {
                data.HeadStyle = headStyle.Value;
            }

            if (tailStyle != null) {
                data.TailStyle = tailStyle.Value;
            }

            return data;
        };
    }

    public static AutoTrestleData CreateAutoTrestleData(TrackSegment trackSegment, AutoTrestle.AutoTrestle.EndStyle headStyle, AutoTrestle.AutoTrestle.EndStyle tailStyle) => CreateOrUpdate(trackSegment, headStyle, tailStyle)(null)!;
}
