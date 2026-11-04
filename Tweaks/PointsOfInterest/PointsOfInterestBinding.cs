using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UI;
using UI.InputRebind;
using UnityEngine.InputSystem;

namespace Tweaks.PointsOfInterest
{
    // Keep IDs stable: vanilla preferences store chords by action ID.
    [HarmonyPatch(typeof(GameInput), "Awake")]
    internal static class PointsOfInterestBinding
    {
        internal const string ActionId = PointsOfInterestBindingDefinition.ActionId;
        private const string ActionName = PointsOfInterestBindingDefinition.ActionName;
        private static InputAction? action;

        [HarmonyPrefix]
        private static void Prefix(GameInput __instance) => AddAction(__instance);

        [HarmonyPostfix]
        private static void Postfix(GameInput __instance) => AddCameraRow(__instance);

        private static void AddAction(GameInput input)
        {
            var asset = input.inputActions;
            action = asset.FindAction(ActionId);
            if (action != null) return;
            // Register before GameInput constructs ChordBindings and loads preferences.
            var map = InputActionMap.FromJson(PointsOfInterestBindingDefinition.Json)[0];
            // Mods may be enabled after the input asset has already been enabled.
            var enabledMaps = asset.actionMaps.Where(item => item.enabled).ToArray();
            asset.Disable();
            try { asset.AddActionMap(map); action = map.FindAction(ActionName, true); }
            finally { foreach (var enabled in enabledMaps) enabled.Enable(); }
        }

        private static void AddCameraRow(GameInput input)
        {
            if (action == null || input.RebindableActions == null) return;
            var tabs = input.RebindableActions;
            for (var i = 0; i < tabs.Length; i++)
                if (tabs[i].title == "Camera")
                    tabs[i].actions = tabs[i].actions.Where(item => item.id.ToString() != ActionId).Concat(new[] { action }).ToArray();
        }

        internal static void InstallExisting()
        {
            var input = GameInput.Shared;
            if (input == null) return;
            AddAction(input);
            // Preserve current bindings when enabling Tweaks after GameInput.Awake.
            if (!input.ChordBindings.Manages(action!))
            {
                // Extend only our default. Rebuilding the manager would turn user-rebound
                // vanilla chords into defaults and break their Reset buttons.
                var field = AccessTools.Field(typeof(ChordBindings), "_defaults")
                    ?? throw new NotSupportedException("Native chord defaults were changed.");
                var defaults = (IDictionary)field.GetValue(input.ChordBindings);
                var added = (IDictionary)field.GetValue(new ChordBindings(input.inputActions));
                defaults.Add(action!.id, added[action.id]);
            }
            AccessTools.Method(typeof(GameInput), "LoadFromPreferences").Invoke(input, null);
            AddCameraRow(input);
            if (input.isActiveAndEnabled) action!.Enable();
        }

        internal static void Disable()
        {
            action?.Disable();
            var input = GameInput.Shared;
            if (input != null && input.RebindableActions != null)
            {
                var tabs = input.RebindableActions;
                for (var i = 0; i < tabs.Length; i++)
                    tabs[i].actions = tabs[i].actions.Where(item => item.id.ToString() != ActionId).ToArray();
            }
            // Leave the action in the asset so other vanilla saves retain its preference.
            action = null;
        }

        [HarmonyPatch(typeof(GameInput), "Update")]
        private static class Poll
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                if (GameInput.MovementInputEnabled && action != null && action.WasPerformedThisFrame())
                    PointsOfInterestFeature.Active?.Runtime.Toggle();
            }
        }
    }
}
