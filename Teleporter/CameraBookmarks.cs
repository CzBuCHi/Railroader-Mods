using System;
using System.Reflection;
using Cameras;
using Helpers;
using UnityEngine;

namespace Teleporter;

internal static class CameraBookmarks
{
    private static readonly FieldInfo Distance = Field("_distance", typeof(float));
    private static readonly FieldInfo Pitch = Field("_angleX", typeof(float));
    private static readonly FieldInfo MovementVelocity = Field("_movementVelocity", typeof(Vector3));
    private static readonly FieldInfo[] Velocities = {
        Field("_distanceVelocity", typeof(float)), Field("_angleXVelocity", typeof(float)),
        Field("_angleYVelocity", typeof(float))
    };
    private static readonly MethodInfo SelectCamera = typeof(CameraSelector).GetMethod("SelectCamera",
        BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(CameraSelector.CameraIdentifier) }, null)
        ?? throw new NotSupportedException("CameraSelector.SelectCamera is unavailable.");

    private static FieldInfo Field(string name, Type type)
    {
        var field = typeof(StrategyCameraController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.FieldType != type)
            throw new NotSupportedException("Strategy camera field differs: " + name);
        return field;
    }

    internal static TeleportLocation Capture()
    {
        var selector = CameraSelector.shared;
        if (selector == null)
            throw new InvalidOperationException("Camera is unavailable.");
        if (selector.CurrentCameraIdentifier == CameraSelector.CameraIdentifier.FirstPerson)
            return new TeleportLocation {
                Position = ToVector(WorldTransformer.WorldToGame(selector.character.GroundPosition)),
                Rotation = ToVector(selector.character.CameraContainer.rotation.eulerAngles),
                FirstPerson = true
            };
        if (selector.CurrentCameraIdentifier != CameraSelector.CameraIdentifier.Strategy)
            throw new InvalidOperationException("Use first-person or tracking view before saving a position.");
        var camera = selector.strategyCamera;
        return new TeleportLocation {
            Position = ToVector(WorldTransformer.WorldToGame(camera.GroundPosition)),
            Rotation = ToVector(camera.transform.rotation.eulerAngles),
            Distance = (float)Distance.GetValue(camera)
        };
    }

    internal static Vector3 GamePosition(TeleportLocation location) => ToVector3(location.Position);

    internal static void Restore(TeleportLocation location)
    {
        var selector = CameraSelector.shared;
        if (selector == null)
            return;
        var mode = location.FirstPerson ? CameraSelector.CameraIdentifier.FirstPerson : CameraSelector.CameraIdentifier.Strategy;
        if (selector.CurrentCameraIdentifier != mode &&
            !(bool)SelectCamera.Invoke(selector, new object[] { mode }))
            throw new InvalidOperationException("Cannot select the saved camera mode.");
        if (location.FirstPerson)
        {
            var rotation = Quaternion.Euler(ToVector3(location.Rotation));
            selector.character.character.motor.BaseVelocity = Vector3.zero;
            selector.character.JumpTo(WorldTransformer.GameToWorld(GamePosition(location)), rotation);
            selector.character.SetRotation(rotation);
            return;
        }
        var camera = selector.strategyCamera;
        var pitch = Mathf.DeltaAngle(0f, location.Rotation.X);
        Pitch.SetValue(camera, Mathf.Clamp(pitch, -30f, 90f));
        Distance.SetValue(camera, Mathf.Clamp(location.Distance, 1f, 500f));
        MovementVelocity.SetValue(camera, Vector3.zero);
        foreach (var field in Velocities)
            field.SetValue(camera, 0f);
        camera.JumpTo(GamePosition(location), Quaternion.Euler(ToVector3(location.Rotation)));
    }

    private static Vector ToVector(Vector3 value) => new Vector { X = value.x, Y = value.y, Z = value.z };
    private static Vector3 ToVector3(Vector value) => new Vector3(value.X, value.Y, value.Z);
}