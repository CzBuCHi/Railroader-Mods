using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using Track;

namespace MapEditor.Features.AutoTrestles;

public static class PatchEditorExtensions
{
    private static string GetTrestleId(TrackSegment trackSegment) => trackSegment.id + "_Trestle";

    public static void AddOrUpdateAutoTrestle(this PatchEditor patchEditor, TrackSegment trackSegment, Func<AutoTrestleData?, AutoTrestleData> addOrUpdate) {
        patchEditor.AddOrUpdateSpliney(GetTrestleId(trackSegment), o => {
            var data = o?.ToObject<AutoTrestleData>();
            data = addOrUpdate(data)!;
            var jData = JObject.FromObject(data);
            jData["handler"] = "StrangeCustoms.AutoTrestleBuilder";
            return jData;
        });
    }

    public static AutoTrestleData? GetAutoTrestle(this PatchEditor patchEditor, TrackSegment trackSegment) {
        Dictionary<string, JObject> splineys;
        try {
            splineys = patchEditor.GetSplineys();
        } catch (InvalidCastException) {
            //System.InvalidCastException: Specified cast is not valid.
            //  at StrangeCustoms.Tracks.PatchEditor+<>c.<GetObject>b__24_1 (Newtonsoft.Json.Linq.JProperty p)
            //  at System.Linq.Enumerable.ToDictionary[TSource,TKey,TElement] (System.Collections.Generic.IEnumerable`1[T] source, System.Func`2[T,TResult] keySelector, System.Func`2[T,TResult] elementSelector, System.Collections.Generic.IEqualityComparer`1[T] comparer)
            //  at System.Linq.Enumerable.ToDictionary[TSource,TKey,TElement] (System.Collections.Generic.IEnumerable`1[T] source, System.Func`2[T,TResult] keySelector, System.Func`2[T,TResult] elementSelector)
            //  at StrangeCustoms.Tracks.PatchEditor.GetObject (Newtonsoft.Json.Linq.JToken obj)
            //  at StrangeCustoms.Tracks.PatchEditor.GetSplineys ()
            splineys = new Dictionary<string, JObject>();
        }

        return splineys.TryGetValue(GetTrestleId(trackSegment), out var data) ? data?.ToObject<AutoTrestleData>() : null;
    }

    public static void RemoveAutoTrestle(this PatchEditor patchEditor, TrackSegment trackSegment) {
        patchEditor.RemoveSpliney(GetTrestleId(trackSegment));
    }
}
