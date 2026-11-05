using HarmonyLib;
using Helpers;
using Model;
using Tweaks.MapTweaks.Rendering;
using Tweaks.MapTweaks;
using UI.CarInspector;
using UI.Map;
using UI;
using UnityEngine;

namespace Tweaks.MapTweaks.UI
{
    [HarmonyPatch(typeof(MapWindow), "OnClick")]
    internal static class MapInfoClick
    {
        [HarmonyPrepare]
        private static bool Prepare() => global::Tweaks.Configuration.Settings.Current.MapTweaks.Enabled;

        [HarmonyPrefix]
        private static bool Prefix(MapWindow __instance, Vector2 viewportNormalizedPoint, MapBuilder ___mapBuilder)
        {
            if (MapTweaksFeature.Active == null) return true;
            if (!GameInput.IsShiftDown || (TrackLocationPicker.Shared != null && TrackLocationPicker.Shared.IsPicking)) return true;
            var camera = ___mapBuilder.mapCamera;
            if (camera == null) return true;
            var ray = camera.ViewportPointToRay(viewportNormalizedPoint);
            var car = __instance.GetComponent<CarMapOverlay>()?.CarAt(ray);
            // Native locomotive click targets also cover icons not yet created by the overlay.
            if (car == null && Physics.Raycast(ray, out var hit, camera.transform.position.y, 1 << Layers.Map))
                car = hit.collider.GetComponentInParent<Car>();
            if (car != null) { global::UI.CarInspector.CarInspector.Show(car); return false; }
            var station = StationAt(camera, ray, viewportNormalizedPoint);
            if (station == null) return true;
            AccessTools.Method(typeof(StationAgent), "ResolveReferences").Invoke(station, null);
            AccessTools.Method(typeof(StationAgent), "UpdateCachedFreightCarList").Invoke(station, null);
            station.Activate(new PickableActivateEvent { Activation = PickableActivation.Primary, IsShiftDown = true });
            return false;
        }

        private static StationAgent? StationAt(UnityEngine.Camera camera, Ray ray, Vector2 point)
        {
            StationAgent? result = null; var best = float.PositiveInfinity;
            foreach (var agent in Object.FindObjectsByType<StationAgent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var position = camera.WorldToViewportPoint(agent.transform.position);
                if (position.z <= 0) continue;
                var delta = new Vector2((position.x - point.x) * camera.pixelWidth, (position.y - point.y) * camera.pixelHeight);
                var score = delta.sqrMagnitude;
                var hit = score <= 14f * 14f;
                // The agent can be a child of the station building. Use its actual
                // footprint as well as a small screen-space target at distant zoom.
                var parent = agent.transform;
                for (var level = 0; level < 3 && parent != null; level++, parent = parent.parent)
                {
                    var renderers = parent.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 80) break; // Never treat a whole town as one station.
                    var buildingFound = false;
                    foreach (var renderer in renderers)
                    {
                        var bounds = renderer.bounds;
                        if (!renderer.enabled || bounds.size.x > 150 || bounds.size.z > 150 ||
                            (new Vector2(bounds.center.x - agent.transform.position.x, bounds.center.z - agent.transform.position.z)).sqrMagnitude > 80f * 80f) continue;
                        buildingFound = true;
                        if (bounds.IntersectRay(ray)) hit = true;
                    }
                    if (buildingFound) break;
                }
                if (hit && score < best) { result = agent; best = score; }
            }
            return result;
        }
    }
}
