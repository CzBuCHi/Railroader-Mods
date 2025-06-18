using System.Collections.Generic;
using System.Linq;
using MapEditor.MapState;
using MapMod.Loaders;
using Model.Ops;
using UnityEngine;

namespace MapEditor.Features.Loaders;

public static class LoaderUtility
{
    public static readonly List<string> Prefabs = [
        "Coal Conveyor",
        "Coal lTower",
        "Diesel Fueling Stand",
        "Water Tower",
        "Water Column"
    ];

    private static readonly List<string> _Prefabs = [
        "coalConveyor",
        "coalTower",
        "dieselFuelingStand",
        "waterTower",
        "waterColumn"
    ];

    public static int GetPrefabIndex(Loader loader) {
        return _Prefabs.IndexOf(loader.Prefab.Replace("vanilla://", ""));
    }

    public static void UpdatePrefab(Loader loader, int index) {
        var value = "vanilla://" + _Prefabs[index];
        if (loader.Prefab == value) {
            return;
        }

        MapStateEditor.NextStep(new LoaderUpdate(loader.Identifier) { Prefab = value });
    }

    private static List<string>? _Industries;

    // TODO: If editor can add industry then this needs to be reevaluated
    public static List<string> Industries => _Industries ??= Object.FindObjectsOfType<Industry>()!.Select(industry => industry.identifier).OrderBy(o => o).ToList();

    public static int GetIndustryIndex(Loader loader) {
        return Industries.IndexOf(loader.Industry);
    }

    public static void UpdateIndustry(Loader loader, int index) {
        var value = Industries[index];
        if (loader.Industry == value) {
            return;
        }

        MapStateEditor.NextStep(new LoaderUpdate(loader.Identifier) { Industry = value });
    }

    public static void Show(Loader loader) => CameraSelector.shared.ZoomToPoint(loader.transform.localPosition);

    public static void Remove(Loader loader) {
        MapStateEditor.NextStep(new LoaderDestroy(loader.Identifier));
    }

    public static LoaderData Destroy(Loader trackNode) {

        var loaderData = new LoaderData();
        loaderData.Read(trackNode);
        loaderData.Destroy(trackNode);
        MapEditorPlugin.PatchEditor!.RemoveNode(trackNode.Identifier);
        return loaderData;
    }

    public static Loader Create(string id, LoaderData loaderData) {
        return loaderData.Create(id);
    }

    public static void Move(Loader loader) {
        var go = new GameObject();
        var picker = go.AddComponent<LoaderPositionPicker>();
        picker.StartPickingLocation(loader);
    }
}
