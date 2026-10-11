using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Track;

namespace TurntableControl;

internal static class NativeAccess
{
    private static readonly FieldInfo NodesField = AccessTools.Field(typeof(Turntable), "nodes");
    private static readonly FieldInfo SpeedField = AccessTools.Field(typeof(TurntableController), "_speed");

    private static readonly FieldInfo BridgeField = AccessTools.Field(typeof(TurntableController), "bridgeTransform");

    internal static void Validate()
    {
        if (BridgeField == null || BridgeField.FieldType != typeof(UnityEngine.Transform) ||
            NodesField == null || NodesField.FieldType != typeof(List<TrackNode>) ||
            SpeedField == null || SpeedField.FieldType != typeof(float) ||
            AccessTools.Field(typeof(TurntableController), "_wasMoving")?.FieldType != typeof(bool) ||
            AccessTools.Method(typeof(TurntableController), "FixedUpdate") == null ||
            AccessTools.Field(typeof(ObjectPicker), "_mouseOverUI")?.FieldType != typeof(bool))
            throw new NotSupportedException("Installed TurntableController API differs from the supported Railroader version.");
    }

    internal static List<TrackNode>? Nodes(Turntable table)
    {
        return NodesField.GetValue(table) as List<TrackNode>;
    }

    internal static UnityEngine.Transform? Bridge(TurntableController controller) =>
        BridgeField.GetValue(controller) as UnityEngine.Transform;

    internal static void StopMotor(TurntableController controller)
    {
        SpeedField.SetValue(controller, 0f);
    }
}
