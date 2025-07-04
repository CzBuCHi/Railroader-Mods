using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Character;
using Game.Persistence;
using Game.State;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using Map.Runtime;
using Network;
using Serilog;
using UI.Builder;
using UI.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MapTest;

// Add 'Map Test' to new game map menu
[PublicAPI]
[HarmonyPatch]
public static class NewGameMenuPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewGameMenu), "BuildMapSelect")]
    public static bool NewGameMenuBuildMapSelect(this NewGameMenu __instance, ref RectTransform __result, UIPanelBuilder builder, string ____progressionId, GameMode ____gameMode) {
        // todo: load from settings
        List<string> values = new List<string> {
            "East Whittier Start",
            "Map Test"
        };
        List<string> progressionIds = new List<string> {
            "ewh",
            "test"
        };
        var num = progressionIds.IndexOf(____progressionId);
        if (num < 0 && ____gameMode == GameMode.Company) {
            num = 0;
            __instance.SelectProgressionId(progressionIds[num]);
        }

        __result = builder.AddDropdown(values, num, i => __instance.SelectProgressionId(progressionIds[i]));
        return false;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(NewGameMenu), "SelectProgressionId")]
    public static void SelectProgressionId(this NewGameMenu __instance, string progressionId) {
        throw new NotImplementedException("It's a stub: NewGameMenu.SelectProgressionId");
    }
}


[PublicAPI]
[HarmonyPatch]
public static class GlobalGameManagerPatches
{
    public static string? ModdedMap;

    // catch and store ProgressionId of modded map
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GlobalGameManager), "_LoadMap")]
    public static void _LoadMap(GlobalGameManager.SceneLoadSetup sceneLoadSetup, GameSetup? gameSetup, INetworkSetup networkSetup) {
        Log.Information("_LoadMap: Prefix");

        if (gameSetup == null) {
            return;
        }

        var newGameSetup = gameSetup.Value.NewGameSetup;
        if (newGameSetup == null) {
            return;
        }

        var progressionId = newGameSetup.Value.ProgressionId;
        if (progressionId == NewGameMenu.ProgressionIdEWH) {
            return;
        }
        
        ModdedMap = progressionId;

        SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
    }

    private static void SceneManagerOnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene) {
        SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;

        var roots = newActiveScene.GetRootGameObjects();
        var sam   = roots.FirstOrDefault(o => o.name == "SceneryAssetManager");
        var world = roots.FirstOrDefault(o => o.name == "World");
        
        if (sam == null || world == null) {
            return;
        }

        //Object.Destroy(sam.gameObject);
        //Object.Destroy(world.gameObject);

    }
}

// replace directoryName for modded map
[PublicAPI]
[HarmonyPatch]
public static class MapManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapManager), "MapPath")]
    public static void MapPath(MapManager __instance) {
        if (GlobalGameManagerPatches.ModdedMap != null) {
            Log.Information("MapManager::directoryName set to '" + GlobalGameManagerPatches.ModdedMap + "'");
            __instance.directoryName = GlobalGameManagerPatches.ModdedMap;
        }
    }
}

public static class W
{
    public static void X() {
        
    }
}