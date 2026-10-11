using System.Collections;
using Game.State;
using Game.Messages;
using HarmonyLib;
using Model;
using Model.AI;
using UI;
using UI.EngineControls;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch(typeof(GameInput), "AutoEngineerSetWaypoint")]
internal static class WaypointShortcutPatch
{
    [HarmonyPrefix]
    private static bool Prefix(GameInput __instance)
    {
        if (!TweaksPlugin.Settings.WaypointShortcutAllModes) return true;
        var locomotive = TrainController.Shared?.SelectedLocomotive;
        if (locomotive == null || StateManager.IsUnloading) return true;
        var persistence = new AutoEngineerPersistence(locomotive.KeyValueObject);
        if (persistence.Orders.Mode == AutoEngineerMode.Waypoint) return true;
        var shortcut = __instance.GetComponent<WaypointShortcut>() ?? __instance.gameObject.AddComponent<WaypointShortcut>();
        shortcut.Begin(locomotive, persistence);
        return false;
    }
}

internal sealed class WaypointShortcut : MonoBehaviour
{
    private Coroutine? pending;
    internal void Begin(BaseLocomotive locomotive, AutoEngineerPersistence persistence)
    {
        if (pending != null) StopCoroutine(pending);
        new AutoEngineerOrdersHelper(locomotive, persistence).SetOrdersValue(mode: AutoEngineerMode.Waypoint);
        pending = StartCoroutine(WaitForMode(locomotive, persistence));
    }
    private IEnumerator WaitForMode(BaseLocomotive locomotive, AutoEngineerPersistence persistence)
    {
        // Multiplayer orders may need a host round trip before the WP controls activate.
        yield return null;
        var deadline = Time.unscaledTime + 5f;
        while (Time.unscaledTime < deadline)
        {
            if (locomotive == null || StateManager.IsUnloading || TrainController.Shared?.SelectedLocomotive != locomotive)
                break;
            if (persistence.Orders.Mode == AutoEngineerMode.Waypoint)
            {
                var controls = AutoEngineerWaypointControls.Shared;
                if (controls != null && controls.isActiveAndEnabled)
                {
                    controls.PresentWaypointPicker(locomotive);
                    break;
                }
            }
            yield return null;
        }
        pending = null;
    }
    private void OnDisable() { StopAllCoroutines(); pending = null; }
}