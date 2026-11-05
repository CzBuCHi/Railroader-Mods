using HarmonyLib;
using TMPro;
using UI.EngineControls;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Hud
{
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
            modes.enabled = Settings.Current.ShowControlModeButtons; if (modes.enabled) modes.Initialize(adapter, dropdown);
            var actions = adapter.GetComponent<ConsistActions>() ?? adapter.gameObject.AddComponent<ConsistActions>();
            actions.enabled = true; actions.Initialize(adapter, speed);
        }
    }
}
