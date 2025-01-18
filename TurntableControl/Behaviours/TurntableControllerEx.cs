using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
//using KeyValue.Runtime;
using Track;
using UnityEngine;

namespace TurntableControl.Behaviours;

[PublicAPI]
public class TurntableControllerEx : MonoBehaviour
{
    private TurntableController _Controller     = null!;
    //private KeyValueObject      _KeyValueObject = null!;

    public void Awake() {
        _Controller = GetComponent<TurntableController>()!;
        //_KeyValueObject = _Controller.GetComponent<KeyValueObject>()!;

        var nodes = Traverse.Create(_Controller.turntable!)!.Field<List<TrackNode>>("nodes")!.Value!;
        foreach (var node in nodes) {
            if (Graph.Shared!.SegmentsConnectedTo(node)!.Count == 0) {
                continue;
            }

            node.gameObject!.AddComponent<TurntableTrackNode>()!.Controller = this;
        }
    }

    private float? _TargetAngle;
    private int    _TargetIndex;

    public void MoveToIndex(int trackNodeIndex) {
        _TargetIndex = trackNodeIndex;
        _TargetAngle = _Controller.turntable!.AngleForIndex(trackNodeIndex);
    }

    public void FixedUpdate() {
        if (!_TargetAngle.HasValue) {
            return;
        }

        _Controller.SetAngle(_TargetAngle.Value);
        _Controller.turntable!.SetStopIndex(_TargetIndex);
        _TargetAngle = null;

        // set lever to 100% -> turntable start rotation
        //_KeyValueObject.Set("controlLever", Value.Float(1));
    }
}
