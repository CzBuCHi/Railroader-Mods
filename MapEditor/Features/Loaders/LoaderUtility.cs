using System.Collections.Generic;
using System.Linq;
using MapEditor.MapState;
using MapMod.Loaders;
using Model.Ops;
using UnityEngine;

namespace MapEditor.Features.Loaders;

public static class LoaderUtility
{
    public static readonly List<string> Prefabs = new List<string>
    {
        "Coal Conveyor",
        "Coal lTower",
        "Diesel Fueling Stand",
        "Water Tower",
        "Water Column"
    };

    private static readonly List<string> _Prefabs = new List<string>
    {
        "coalConveyor",
        "coalTower",
        "dieselFuelingStand",
        "waterTower",
        "waterColumn"
    };

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
}
