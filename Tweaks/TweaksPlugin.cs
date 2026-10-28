using System;
using HarmonyLib;
using Railroader.Modding;
using TMPro;
using UI.EngineControls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks
{
    public sealed class TweaksPlugin : IModEntryPoint, IWorldLifecycle
    {
        private const string HarmonyId = "CzBuCHi.Tweaks.Native";
        private Harmony? harmony;

        public void OnModLoad(IModContext context)
        {
            if (harmony != null) return;
            harmony = new Harmony(HarmonyId);
            try
            {
                harmony.PatchAll(typeof(TweaksPlugin).Assembly);
                context.Log.Info("Loaded selected Tweaks 3.0.1 features.");
            }
            catch { harmony.UnpatchAll(HarmonyId); harmony = null; throw; }
        }

        public void OnWorldLoad()
        {
            // Also handle HUDs enabled before the mod's world-load hook.
            foreach (var adapter in Object.FindObjectsByType<LocomotiveControlsUIAdapter>(FindObjectsSortMode.None))
                HudPatch.Install(adapter);
        }

        public void OnWorldUnload() => CleanupHud();
        public void OnModUnload()
        {
            harmony?.UnpatchAll(HarmonyId); harmony = null;
            CleanupHud();
        }

        private static void CleanupHud()
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<ControlModeButtons>())
            { component.enabled = false; Object.Destroy(component); }
            foreach (var component in Resources.FindObjectsOfTypeAll<ConsistActions>())
            { component.enabled = false; Object.Destroy(component); }
        }
    }

    [HarmonyPatch(typeof(LocomotiveControlsUIAdapter), "OnEnable")]
    internal static class HudPatch
    {
        [HarmonyPostfix]
        private static void Postfix(LocomotiveControlsUIAdapter __instance) => Install(__instance);

        internal static void Install(LocomotiveControlsUIAdapter adapter)
        {
            var dropdown = (TMP_Dropdown)AccessTools.Field(typeof(LocomotiveControlsUIAdapter), "modeDropdown").GetValue(adapter);
            var speed = (TMP_Text)AccessTools.Field(typeof(LocomotiveControlsUIAdapter), "speedLabel").GetValue(adapter);
            var modes = adapter.GetComponent<ControlModeButtons>() ?? adapter.gameObject.AddComponent<ControlModeButtons>();
            modes.enabled = true; modes.Initialize(adapter, dropdown);
            var actions = adapter.GetComponent<ConsistActions>() ?? adapter.gameObject.AddComponent<ConsistActions>();
            actions.enabled = true; actions.Initialize(adapter, speed);
        }
    }
}
