using AlinasMapMod;
using UnityEngine;

namespace MapMod.Loaders;

public sealed class LoaderData
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public string  Prefab   { get; set; } = "empty://";
    public string  Industry { get; set; } = "";

    public LoaderInstance Create(string id) {
        var go = new GameObject(id);
        go.transform.parent = Utility.GetParent("Loaders").transform;
        var comp = go.AddComponent<LoaderInstance>();
        comp.name = id;
        comp.Identifier = id;
        Write(comp);
        return comp;
    }

    public void Destroy(LoaderInstance comp) {
        Object.Destroy(comp.gameObject);
    }

    public void Read(LoaderInstance comp) {
        Position = comp.transform.localPosition;
        Rotation = comp.transform.localEulerAngles;
        Prefab = comp.Prefab;
        Industry = comp.Industry;
    }

    public void Write(LoaderInstance comp) {
        comp.transform.localPosition = Position;
        comp.transform.localEulerAngles = Rotation;
        comp.Prefab = Prefab;
        comp.Industry = Industry;
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
