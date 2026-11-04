using System;
using System.Reflection;
using Cameras;
using Helpers;
using UnityEngine;

namespace Tweaks.PointsOfInterest
{
    internal static class CameraBookmarks
    {
        private static readonly MethodInfo SelectCamera = typeof(CameraSelector).GetMethod("SelectCamera",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(CameraSelector.CameraIdentifier) }, null)
            ?? throw new NotSupportedException("CameraSelector.SelectCamera was changed.");
        private static readonly FieldInfo MovementVelocity = Field("_movementVelocity", typeof(Vector3));
        private static readonly FieldInfo[] Velocities = {
            Field("_distanceVelocity", typeof(float)), Field("_angleXVelocity", typeof(float)),
            Field("_angleYVelocity", typeof(float))
        };

        private static FieldInfo Field(string name, Type type)
        {
            var field = typeof(StrategyCameraController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != type)
                throw new NotSupportedException("Strategy camera motion field was changed: " + name);
            return field;
        }

        internal static void Validate() { _ = MovementVelocity; _ = Velocities; _ = SelectCamera; }
        internal static TeleportLocation Capture()
        {
            var selector = CameraSelector.shared;
            if (selector == null) throw new InvalidOperationException("Camera is unavailable.");
            if (selector.CurrentCameraIdentifier == CameraSelector.CameraIdentifier.FirstPerson)
                return new TeleportLocation
                {
                    Position = ToVector(selector.character.GroundPosition.WorldToGame()),
                    Rotation = ToVector(selector.character.CameraContainer.rotation.eulerAngles),
                    FirstPerson = true
                };
            if (selector.CurrentCameraIdentifier != CameraSelector.CameraIdentifier.Strategy)
                throw new InvalidOperationException("Use first-person or tracking view before saving a position.");
            var camera = selector.strategyCamera;
            var state = camera.CaptureState();
            // Store absolute bookmarks even when the camera is following a moving car.
            return new TeleportLocation
            {
                Position = ToVector(camera.GroundPosition.WorldToGame()),
                Rotation = new Vector { X = state.AngleX, Y = camera.transform.rotation.eulerAngles.y },
                Distance = state.Distance
            };
        }

        internal static Vector3 GamePosition(TeleportLocation location) => ToVector3(location.Position);

        internal static void Restore(TeleportLocation location)
        {
            var selector = CameraSelector.shared;
            if (selector == null) throw new InvalidOperationException("Camera is unavailable.");
            var mode = location.FirstPerson ? CameraSelector.CameraIdentifier.FirstPerson : CameraSelector.CameraIdentifier.Strategy;
            if (selector.CurrentCameraIdentifier != mode && !(bool)SelectCamera.Invoke(selector, new object[] { mode }))
                throw new InvalidOperationException("Cannot select the saved camera mode.");
            if (location.FirstPerson)
            {
                var rotation = Quaternion.Euler(ToVector3(location.Rotation));
                selector.character.character.motor.BaseVelocity = Vector3.zero;
                selector.character.JumpTo(GamePosition(location).GameToWorld(), rotation);
                selector.character.SetRotation(rotation);
            }
            else
            {
                var camera = selector.strategyCamera;
                // Native Restore handles the view, but leaves the previous movement inertia intact.
                MovementVelocity.SetValue(camera, Vector3.zero);
                foreach (var field in Velocities) field.SetValue(camera, 0f);
                if (!camera.Restore(new StrategyCameraState(GamePosition(location), null!,
                    Mathf.Clamp(location.Distance, 1f, 500f), Mathf.Clamp(Mathf.DeltaAngle(0, location.Rotation.X), -30f, 90f), location.Rotation.Y)))
                    throw new InvalidOperationException("Could not restore the saved tracking view.");
            }
        }

        private static Vector ToVector(Vector3 value) => new Vector { X = value.x, Y = value.y, Z = value.z };
        private static Vector3 ToVector3(Vector value) => new Vector3(value.X, value.Y, value.Z);
    }
}
