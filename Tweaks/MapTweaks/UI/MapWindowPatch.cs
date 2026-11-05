using HarmonyLib;
using Tweaks.MapTweaks.Rendering;
using Tweaks.MapTweaks;
using UI.Map;
using UnityEngine.UI;

namespace Tweaks.MapTweaks.UI
{
    [HarmonyPatch(typeof(MapWindow), "Start")]
    internal static class MapWindowPatch
    {
        [HarmonyPrepare]
        private static bool Prepare() => global::Tweaks.Configuration.Settings.Current.MapTweaks.Enabled;

        [HarmonyPostfix]
        private static void Postfix(MapWindow __instance) => Install(__instance);

        internal static void Install(MapWindow window)
        {
            if (MapTweaksFeature.Active == null) return;
            var image = (RawImage)AccessTools.Field(typeof(MapWindow), "rawImage").GetValue(window);
            var builder = (MapBuilder)AccessTools.Field(typeof(MapWindow), "mapBuilder").GetValue(window);
            if (image == null || builder == null) return;
            var overlay = window.GetComponent<CarMapOverlay>() ?? window.gameObject.AddComponent<CarMapOverlay>();
            overlay.Initialize(image, builder);
            var controls = window.GetComponent<MapControls>() ?? window.gameObject.AddComponent<MapControls>();
            controls.Initialize(builder);
        }
    }

}
