using System.Collections.Generic;
using System.Linq;
using Cameras;
using HarmonyLib;
using Helpers;
using Model;
using RollingStock;
using Track;
using UnityEngine;

namespace Tweaks
{
    internal static class MaintenanceCameraFocus
    {
        private static Car? target;
        private static Car.LogicalEnd? airEnd;
        private static Collider? oilPoint;
        private static readonly System.Reflection.FieldInfo TargetHeight = AccessTools.Field(typeof(StrategyCameraController), "_targetHeight");

        internal static void Clear() { target = null; airEnd = null; oilPoint = null; }

        internal static void FollowAir(Car car, Car.LogicalEnd end)
        {
            var selector = CameraSelector.shared;
            if (selector == null) return;
            selector.FollowCar(car); // Native setter clears any previous maintenance focus.
            target = car; airEnd = end; oilPoint = null;
        }

        internal static void FollowOil(Car car)
        {
            var selector = CameraSelector.shared;
            if (selector == null) return;
            selector.FollowCar(car);
            target = car; airEnd = null; oilPoint = null;
        }

        internal static (Vector3 position, Quaternion rotation) Adjust(
            (Vector3 position, Quaternion rotation) pose, StrategyCameraController camera)
        {
            if (target == null || camera.FollowCar != target || Graph.Shared == null) return pose;
            Vector3 point;
            if (airEnd.HasValue)
            {
                var location = airEnd == Car.LogicalEnd.A ? target.LocationA : target.LocationB;
                if (!location.IsValid) return pose;
                point = WorldTransformer.GameToWorld(Graph.Shared.Current.GetPosition(location)) + Vector3.up * 0.9f;
            }
            else
            {
                // Models can stream in after the click. Resolve the actual bearing
                // hitbox then, and keep that same moving point while following.
                if (oilPoint == null)
                    oilPoint = target.GetComponentsInChildren<OilPointPickable>()
                        .SelectMany(p => p.GetComponents<Collider>()).Where(c => c.enabled)
                        .OrderBy(c => (c.bounds.center - camera.transform.position).sqrMagnitude).FirstOrDefault();
                if (oilPoint != null) point = oilPoint.bounds.center;
                else
                {
                    var location = target.TruckLocationA;
                    if (!location.IsValid) return pose;
                    point = WorldTransformer.GameToWorld(Graph.Shared.Current.GetPosition(location)) + Vector3.up * 0.5f;
                }
            }
            // Native code adds the normal body-height offset and follow inertia
            // after this call. Replace only the body anchor, keeping zoom/orbit.
            return (point - Vector3.up * (float)TargetHeight.GetValue(camera), pose.rotation);
        }
    }

    [HarmonyPatch(typeof(StrategyCameraController), "set_FollowCar")]
    internal static class ClearMaintenanceCameraFocus
    {
        [HarmonyPrefix]
        private static void Prefix() => MaintenanceCameraFocus.Clear();
    }

    [HarmonyPatch(typeof(StrategyCameraController), "UpdateCameraPosition")]
    internal static class MaintenanceCameraFocusPatch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => FollowAnchorRewrite.Apply(instructions, AccessTools.Method(typeof(Car), "GetMoverTargetPositionRotation"),
                AccessTools.Method(typeof(MaintenanceCameraFocus), nameof(MaintenanceCameraFocus.Adjust)));
    }
}
