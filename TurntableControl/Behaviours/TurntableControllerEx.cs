using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Serilog;
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

        var activeIndexes = new HashSet<int>();
        for (var i = 0; i < nodes.Count; i++) {
            var j = (i + nodes.Count / 2) % nodes.Count;
            if (Graph.Shared.SegmentsConnectedTo(nodes[i]!).Count == 0 &&
                Graph.Shared.SegmentsConnectedTo(nodes[j]!).Count == 0
                ) {
                Log.Information("Node: " + nodes[i] + " - SKIP " + i + ", " + j);
                continue;
            }

            Log.Information("Node: " + nodes[i] + " - TAKE " + i + ", " + j);
            activeIndexes.Add(i);
            activeIndexes.Add(j);
        }

        foreach (var i in activeIndexes) {
            nodes[i]!.gameObject.AddComponent<TurntableTrackNode>().Controller = this;
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
