using System;
using Railroader.Modding;
using Tweaks.MapTweaks.Configuration;
using Tweaks.MapTweaks.Rendering;
using Tweaks.MapTweaks.Switches;
using Tweaks.MapTweaks.UI;
using UI.Map;
using UnityEngine;
using Object = UnityEngine.Object;
using Settings = global::Tweaks.MapTweaks.Configuration.Settings;

namespace Tweaks.MapTweaks
{
    internal sealed class MapTweaksFeature
    {
        internal static MapTweaksFeature? Active { get; private set; }
        internal SwitchActions Switches { get; private set; } = null!;
        private readonly MapSettingsStore settings = new MapSettingsStore(global::Tweaks.Configuration.Settings.FilePath);
        private IModContext context = null!;
        internal static VehicleDisplayMode VehicleDisplayMode
        {
            get => global::Tweaks.Configuration.Settings.Current.MapTweaks.VehicleDisplayMode;
            set => Active?.Change(settings => settings.VehicleDisplayMode = value);
        }
        internal static bool FollowSelectedCar
        {
            get => global::Tweaks.Configuration.Settings.Current.MapTweaks.FollowSelectedCar;
            set => Active?.Change(settings => settings.FollowSelectedCar = value);
        }
        internal void Change(Action<Settings> change)
        {
            try { settings.Change(change); }
            catch (Exception error)
            {
                context.Log.Error(error, "Cannot save map settings.");
                global::UI.Common.Toast.Present("Could not save map settings.");
            }
        }

        public void OnModLoad(IModContext context)
        {
            if (Active != null || !global::Tweaks.Configuration.Settings.Current.MapTweaks.Enabled) return;
            this.context = context;
            try
            {
                Active = this;
                Switches = new SwitchActions(context.Log);
                context.Log.Info("Loaded map tweaks.");
            }
            catch { OnModUnload(); throw; }
        }

        public void OnWorldLoad()
        {
            if (Active != this) return;
            foreach (var window in Object.FindObjectsByType<MapWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                MapWindowPatch.Install(window);
        }

        public void OnWorldUnload() => Cleanup();
        public void OnModUnload()
        {
            Cleanup();
            if (Active == this) Active = null;
        }

        private static void Cleanup()
        {
            foreach (var controls in Object.FindObjectsByType<MapControls>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { controls.Shutdown(); Object.Destroy(controls); }
            foreach (var overlay in Object.FindObjectsByType<CarMapOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                overlay.Shutdown();
                Object.Destroy(overlay);
            }
        }
    }

}
