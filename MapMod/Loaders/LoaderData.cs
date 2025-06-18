using AlinasMapMod;
using Serilog;
using Track;
using UnityEngine;

namespace MapMod.Loaders;

public sealed class LoaderData
{
    public string           Handler         { get; }      = typeof(LoaderBuilder).FullName!;
    public string           Segment         { get; set; } = null!;
    public float            Distance        { get; set; }
    public TrackSegment.End End             { get; set; }
    public bool             FlipOrientation { get; set; }
    public string           Prefab          { get; set; } = "empty://";
    public string           Industry        { get; set; } = "";

    public Loader Create(string id) {
        var go = new GameObject(id);
        go.transform.parent = Utility.GetParent("Loaders").transform;
        var comp = go.AddComponent<Loader>();
        comp.name = id;
        comp.Identifier = id;
        Write(comp);
        return comp;
    }

    public void Destroy(Loader comp) {
        Object.Destroy(comp.gameObject);
    }

    public void Read(Loader comp) {
        Segment = comp.Location.segment.id;
        Distance = comp.Location.distance;
        End = comp.Location.end;
        Prefab = comp.Prefab;
        Industry = comp.Industry;
    }

    public void Write(Loader comp) {
        comp.Location = new Location(Graph.Shared.GetSegment(Segment), Distance, End);
        comp.Prefab = Prefab;
        comp.Industry = Industry;
        comp.FlipOrientation = FlipOrientation;
    }

    public void Validate() {
        if (!Prefab.Contains("://")) {
            throw new ValidationException("Prefab must be a valid URI  " + Prefab);
        }

        if (Industry == "" && (Prefab.Contains("coal") || Prefab.Contains("diesel"))) {
            throw new ValidationException("Industry required for prefab " + Prefab);
        }
    }
}
