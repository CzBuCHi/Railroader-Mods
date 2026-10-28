using System;
using HarmonyLib;
using Railroader.Modding;
using UI.Map;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WaybillMap
{
    public sealed class WaybillMapPlugin : IModEntryPoint, IWorldLifecycle
    {
        private const string HarmonyId = "CzBuCHi.WaybillMap.Native";
        internal static MapDisplayOption CarsOption { get; private set; } = null!;
        private IDisposable? displayRegistration;
        private Harmony? harmony;

        public void OnModLoad(IModContext context)
        {
            if (harmony != null) return;
            try
            {
                CarsOption = new MapDisplayOption("czbuchi/waybill-map/cars", "Waybill cars", true);
                displayRegistration = MapDisplayOptions.Add(CarsOption);
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(typeof(WaybillMapPlugin).Assembly);
                context.Log.Info("Loaded WaybillMap 1.0.0. Toggle Waybill cars in Map > Display.");
            }
            catch { OnModUnload(); throw; }
        }

        public void OnWorldLoad()
        {
            foreach (var window in Object.FindObjectsByType<MapWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                MapWindowPatch.Install(window);
        }

        public void OnWorldUnload() => Cleanup();
        public void OnModUnload()
        {
            harmony?.UnpatchAll(HarmonyId); harmony = null;
            Cleanup();
            displayRegistration?.Dispose(); displayRegistration = null;
        }

        private static void Cleanup()
        {
            foreach (var overlay in Object.FindObjectsByType<CarMapOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                overlay.Shutdown();
                Object.Destroy(overlay);
            }
        }
    }

    [HarmonyPatch(typeof(MapWindow), "Start")]
    internal static class MapWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapWindow __instance) => Install(__instance);

        internal static void Install(MapWindow window)
        {
            var image = (RawImage)AccessTools.Field(typeof(MapWindow), "rawImage").GetValue(window);
            var builder = (MapBuilder)AccessTools.Field(typeof(MapWindow), "mapBuilder").GetValue(window);
            if (image == null || builder == null) return;
            var overlay = window.GetComponent<CarMapOverlay>() ?? window.gameObject.AddComponent<CarMapOverlay>();
            overlay.Initialize(image, builder);
        }
    }
}
