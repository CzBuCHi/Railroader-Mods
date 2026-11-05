using HarmonyLib;
using Railroader.Modding;
using Tweaks.Camera;
using Tweaks.Hud;
using UI.EngineControls;
using UnityEngine;
using Object = UnityEngine.Object;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks
{
    public sealed class TweaksPlugin : IModEntryPoint, IWorldLifecycle
    {
        private const string HarmonyId = "CzBuCHi.Tweaks.Native";
        private Harmony? harmony;
        private readonly PointsOfInterest.PointsOfInterestFeature points = new PointsOfInterest.PointsOfInterestFeature();

        private readonly MapTweaks.MapTweaksFeature map = new MapTweaks.MapTweaksFeature();

        public void OnModLoad(IModContext context)
        {
            if (harmony != null) return;
            Settings.Current = Settings.Load(Settings.FilePath, context.Log.Warning);
            harmony = new Harmony(HarmonyId);
            try
            {
                harmony.PatchAll(typeof(TweaksPlugin).Assembly);
                points.OnModLoad(context);
                map.OnModLoad(context);
                context.Log.Info("Loaded Tweaks 3.0.21 features.");
            }
            catch { OnModUnload(); throw; }
        }

        public void OnWorldLoad()
        {
            points.OnWorldLoad();
            map.OnWorldLoad();
            // Also handle HUDs enabled before the mod's world-load hook.
            foreach (var adapter in Object.FindObjectsByType<LocomotiveControlsUIAdapter>(FindObjectsSortMode.None))
                HudPatch.Install(adapter);
        }

        public void OnWorldUnload() { points.OnWorldUnload(); map.OnWorldUnload(); CleanupHud(); }
        public void OnModUnload()
        {
            map.OnModUnload();
            points.OnModUnload();
            harmony?.UnpatchAll(HarmonyId); harmony = null;
            CleanupHud();
        }

        private static void CleanupHud()
        {
            MaintenanceCameraFocus.Clear();
            foreach (var component in Resources.FindObjectsOfTypeAll<ControlModeButtons>())
            { component.enabled = false; Object.Destroy(component); }
            foreach (var component in Resources.FindObjectsOfTypeAll<ConsistActions>())
            { component.enabled = false; Object.Destroy(component); }
        }
    }

}
