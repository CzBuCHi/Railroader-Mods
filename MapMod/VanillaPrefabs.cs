using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapMod;

[PublicAPI]
internal static class VanillaPrefabs
{
    private static readonly Dictionary<string, GameObject> _Prefabs = new();

    public static string[] AvailablePrefabs => [
        .. AvailableRoundhousePrefabs,
        .. AvailableLoaderPrefabs,
        .. AvailableStationPrefabs
    ];

    public static string[] AvailableRoundhousePrefabs => [
        "roundhouseStall",
        "roundhouseStart",
        "roundhouseEnd"
    ];

    public static string[] AvailableLoaderPrefabs => [
        "coalConveyor",
        "coalTower",
        "dieselFuelingStand",
        "waterTower",
        "waterColumn"
    ];

    public static string[] AvailableStationPrefabs => [
        "flagStopStation",
        "brysonDepot",
        "dillsboroDepot",
        "southernCombinationDepot"
    ];

    public static GameObject GetPrefab(string key) {
        if (!_Prefabs.ContainsKey(key)) {
            var prefab = key switch {
                "roundhouseStall"          => GenerateRoundhouseStallPrefab(),
                "roundhouseStart"          => GenerateRoundhouseEndPrefab(false),
                "roundhouseEnd"            => GenerateRoundhouseEndPrefab(true),
                "coalConveyor"             => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Coal Conveyor")!),
                "coalTower"                => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Coaling Tower")!),
                "dieselFuelingStand"       => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/East Whittier Diesel Fueling Stand")!),
                "waterTower"               => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Whittier Water Tower")!),
                "waterColumn"              => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Water Column")!),
                "flagStopStation"          => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Ela/flagstopstation")!),
                "brysonDepot"              => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Depot")!),
                "dillsboroDepot"           => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Dillsboro/Dillsboro Depot")!),
                "southernCombinationDepot" => Clone(Utility.GameObjectFromUri("path://scene/World/Large Scenery/Whittier/Southern Combination Depot")!),
                _                          => throw new ArgumentException("Attempted to load unknown vanilla prefab: " + key)
            };
            _Prefabs.Add(key, prefab);
        }

        return _Prefabs[key]!;
    }

    public static void ClearCache() {
        foreach (var prefab in _Prefabs.Values) {
            Object.Destroy(prefab);
        }

        _Prefabs.Clear();
    }

    private static GameObject Clone(GameObject go) {
        var ngo    = Object.Instantiate(go)!;
        ngo.SetActive(false);
        ngo.transform.localPosition = Vector3.zero;
        ngo.transform.localEulerAngles = Vector3.zero;
        return ngo;
    }

    private static GameObject GenerateRoundhouseStallPrefab() {
        var go            = new GameObject("Roundhouse Stall");
        var stallPrefab   = Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Stall");
        var betweenPrefab = Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Between");

        var stall = Object.Instantiate(stallPrefab, go.transform)!;
        stall.transform.localPosition = Vector3.zero;
        stall.transform.localEulerAngles = new Vector3(0, 0, 0);
        stall.transform.localScale = new Vector3(1, 1, 1);

        var between = Object.Instantiate(betweenPrefab, go.transform)!;
        between.transform.localPosition = Vector3.zero;
        between.transform.localEulerAngles = new Vector3(270, 180 - 11.25f / 2, 0);

        return go;
    }

    private static GameObject GenerateRoundhouseEndPrefab(bool start) {
        var stallPrefab   = Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Stall");
        var endPrefab     = Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Side");
        var betweenPrefab = Utility.GameObjectFromUri("path://scene/World/Large Scenery/Bryson/Bryson Turntable/Roundhouse/Roundhouse Modular A Between");

        var go    = new GameObject("Roundhouse End");
        var stall = Object.Instantiate(stallPrefab, go.transform)!;
        stall.transform.localPosition = Vector3.zero;
        stall.transform.localEulerAngles = new Vector3(0, 0, 0);
        stall.transform.localScale = new Vector3(1, 1, 1);

        var end = Object.Instantiate(endPrefab, go.transform)!;
        end.transform.localPosition = Vector3.zero;
        end.transform.localEulerAngles = new Vector3(0, 180, 0);
        end.transform.localScale = new Vector3(start ? 1 : -1, 1, 1);

        if (!start) {
            var between = Object.Instantiate(betweenPrefab, go.transform)!;
            between.transform.localPosition = Vector3.zero;
            between.transform.localEulerAngles = new Vector3(270, 180 + 11.25f / 2, 0);
        }

        return go;
    }
}
