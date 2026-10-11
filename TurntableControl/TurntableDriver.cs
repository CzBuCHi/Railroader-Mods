using System;
using System.Collections.Generic;
using Game.State;
using Game.Messages;
using KeyValue.Runtime;
using Track;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TurntableControl;

internal sealed class TurntableDriver : MonoBehaviour
{
    private const string TargetKey = "CzBuCHi.TurntableControl.targetIndex";
    private const string LeverKey = "controlLever";
    private readonly Dictionary<int, TurntableMarker> markers = new Dictionary<int, TurntableMarker>();
    private TurntableController? controller;
    private Turntable? table;
    private KeyValueObject? properties;
    private IDisposable? targetObserver;
    private IDisposable? leverObserver;
    private Material? markerMaterial;
    private GameObject? bridgeArrow;
    private RotationGoal? goal;
    private int targetIndex;
    private int? pendingIndex;
    private bool pendingStop;
    private bool manualOverride;
    private bool writingLever;
    private bool initialized;
    private bool stepActive;
    private bool expectedMovement;
    private float angleBeforeStep;

    internal void Initialize(TurntableController nativeController)
    {
        controller = nativeController;
        table = controller.turntable;
        properties = controller.GetComponent<KeyValueObject>();
        if (properties == null || string.IsNullOrEmpty(properties.RegisteredId))
            throw new InvalidOperationException("Turntable property object is not registered.");
        if (NativeAccess.Nodes(table) == null)
            throw new InvalidOperationException("Turntable track nodes are unavailable.");

        // Saved requests are not resumed automatically after loading a map.
        if (StateManager.IsHost && !properties[TargetKey].IsNull)
        {
            SetLever(0.5f);
            NativeAccess.StopMotor(controller);
            properties[TargetKey] = Value.Null();
        }
        targetObserver = properties.Observe(TargetKey, OnTargetChanged, callInitial: false);
        leverObserver = properties.Observe(LeverKey, OnLeverChanged, callInitial: false);
        CreateBridgeArrow();
        initialized = true;
        RefreshMarkers();
    }

    internal Vector3 Center => table != null ? table.transform.position : transform.position;

    internal bool CanStayAttached => initialized && controller != null && table != null && controller.turntable == table;

    private bool Ready => initialized && controller != null && controller.isActiveAndEnabled &&
        table != null && controller.turntable == table && properties != null &&
        StateManager.Shared != null && !StateManager.IsUnloading;

    private void OnTargetChanged(Value value)
    {
        if (!StateManager.IsHost)
            return;
        if (value.IsNull)
        {
            pendingStop = true;
            pendingIndex = null;
        }
        else if (value.Type == KeyValue.Runtime.ValueType.Int)
        {
            manualOverride = false;
            pendingIndex = value.IntValue;
            pendingStop = false;
        }
    }

    private void OnLeverChanged(Value value)
    {
        if (StateManager.IsHost && !writingLever && (goal != null || pendingIndex.HasValue))
            manualOverride = true;
    }

    internal void RequestMove(int index, bool stop)
    {
        if (!Ready || properties == null ||
            !StateManager.CheckAuthorizedToChangeProperty(properties.RegisteredId, TargetKey))
            return;

        if (stop)
        {
            properties[TargetKey] = Value.Null();
            return;
        }
        if (IsAvailableIndex(index))
            properties[TargetKey] = Value.Int(index);
    }

    private bool IsAvailableIndex(int index)
    {
        if (table == null)
            return false;
        var nodes = NativeAccess.Nodes(table);
        if (nodes == null || nodes.Count != table.subdivisions || nodes.Count % 2 != 0 ||
            index < 0 || index >= nodes.Count)
            return false;
        var opposite = (index + nodes.Count / 2) % nodes.Count;
        return HasTrack(nodes[index]) || HasTrack(nodes[opposite]);
    }

    private bool HasTrack(TrackNode node)
    {
        if (node == null || !node.gameObject.activeInHierarchy)
            return false;
        var graph = node.GetComponentInParent<Graph>();
        if (graph == null)
            return false;
        foreach (var segment in graph.SegmentsConnectedTo(node))
            if (segment != null && segment.turntable != table)
                return true;
        return false;
    }

    internal void BeforeNativeStep(float nativeSpeed)
    {
        stepActive = false;
        if (!StateManager.IsHost)
            return;
        try
        {
            if (!Ready)
            {
                if (goal != null || pendingIndex.HasValue)
                {
                    Cancel(resetLever: true);
                    if (controller != null)
                        NativeAccess.StopMotor(controller);
                }
                return;
            }
            if (manualOverride)
            {
                // Retain the lever position chosen by the player.
                manualOverride = false;
                Cancel(resetLever: false);
                return;
            }
            if (pendingStop)
            {
                pendingStop = false;
                Cancel(resetLever: true);
                if (controller != null)
                    NativeAccess.StopMotor(controller);
                return;
            }
            if (pendingIndex.HasValue)
            {
                var index = pendingIndex.Value;
                pendingIndex = null;
                if (!IsAvailableIndex(index))
                {
                    Cancel(resetLever: true);
                    return;
                }
                targetIndex = index;
                goal = new RotationGoal(table!.Angle, table.AngleForIndex(index));
                SetLever(goal.Direction > 0 ? 1f : 0f);
            }
            if (goal == null)
                return;
            if (!IsAvailableIndex(targetIndex))
            {
                Cancel(resetLever: true);
                NativeAccess.StopMotor(controller!);
                return;
            }
            expectedMovement = Mathf.Abs(Mathf.Lerp(nativeSpeed, goal.Direction * 10f, Time.deltaTime)) > 0.5f;
            angleBeforeStep = table!.Angle;
            stepActive = true;
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    internal void AfterNativeStep(ref float speed, ref bool wasMoving)
    {
        if (!stepActive || !StateManager.IsHost || goal == null || !Ready)
            return;
        stepActive = false;
        try
        {
            if (goal.ReachedDuringStep(angleBeforeStep, table!.Angle))
            {
                // Clamp only the final native physics step; never jump to a distant angle.
                controller!.SetAngle(goal.TargetAngle);
                speed = 0f;
                wasMoving = false;
                table.SetStopIndex(targetIndex);
                StateManager.ApplyLocal(new TurntableUpdateStopIndex(table.id, StateManager.Now, goal.TargetAngle, targetIndex));
                Cancel(resetLever: true);
                Debug.Log("[TurntableControl] Arrived at track " + targetIndex + " on " + table.id);
            }
            else if (expectedMovement && Math.Abs(speed) < 0.0001f &&
                Math.Abs(RotationGoal.SignedDelta(angleBeforeStep, table!.Angle)) < 0.0001f)
            {
                // Native collision/vehicle checks stopped the motor. Do not bypass them or restart it.
                Cancel(resetLever: true);
                Debug.LogWarning("[TurntableControl] Native turntable movement was blocked; automatic movement cancelled.");
            }
        }
        catch (Exception error)
        {
            speed = 0f;
            Fail(error);
        }
    }

    private void SetLever(float value)
    {
        if (properties == null)
            return;
        writingLever = true;
        try
        {
            // Native property range 0..1 corresponds to the cab lever -100%..+100%.
            properties[LeverKey] = Value.Float(value);
        }
        finally
        {
            writingLever = false;
        }
    }

    private void Cancel(bool resetLever)
    {
        goal = null;
        pendingIndex = null;
        pendingStop = false;
        stepActive = false;
        if (properties == null || StateManager.Shared == null || StateManager.IsUnloading)
            return;
        if (resetLever)
            SetLever(0.5f);
        if (!properties[TargetKey].IsNull)
            properties[TargetKey] = Value.Null();
        pendingStop = false;
    }

    private void Fail(Exception error)
    {
        goal = null;
        stepActive = false;
        try
        {
            Cancel(resetLever: true);
            if (controller != null)
                NativeAccess.StopMotor(controller);
        }
        catch (Exception stopError)
        {
            Debug.LogError("[TurntableControl] Could not stop automatic movement: " + stopError);
        }
        Debug.LogError("[TurntableControl] Automatic movement failed: " + error);
    }

    internal void RefreshMarkers()
    {
        if (!Ready || table == null)
            return;
        var nodes = NativeAccess.Nodes(table);
        if (nodes == null)
            return;
        var obsolete = new List<int>();
        foreach (var pair in markers)
        {
            if (pair.Value == null || !IsAvailableIndex(pair.Key) || pair.Value.Node != nodes[pair.Key])
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    Object.Destroy(pair.Value.gameObject);
                }
                obsolete.Add(pair.Key);
            }
        }
        foreach (var index in obsolete)
            markers.Remove(index);
        for (var i = 0; i < nodes.Count; i++)
        {
            if (markers.ContainsKey(i) || !IsAvailableIndex(i))
                continue;
            var markerObject = new GameObject("CzBuCHi.TurntableControl.Track." + i);
            markerObject.transform.SetParent(nodes[i].transform, false);
            markerObject.transform.localPosition = Vector3.up * 0.08f;
            var marker = markerObject.AddComponent<TurntableMarker>();
            marker.Initialize(this, nodes[i], i, MarkerMaterial());
            markers.Add(i, marker);
        }
    }

    private void CreateBridgeArrow()
    {
        var bridge = NativeAccess.Bridge(controller!);
        if (bridge == null)
            throw new InvalidOperationException("Turntable bridge transform is unavailable.");
        bridgeArrow = new GameObject("CzBuCHi.TurntableControl.BridgeDirection");
        bridgeArrow.transform.SetParent(bridge, false);
        // The native controller rotates bridge +Z toward AngleForIndex(target).
        // Put the arrow inside that end of the bridge, above the rail plane.
        var center = bridge.InverseTransformPoint(Center);
        bridgeArrow.transform.localPosition = center +
            Vector3.forward * Mathf.Max(0f, table!.radius - 0.95f) + Vector3.up * 0.08f;
        TurntableArrow.Create(bridgeArrow, MarkerMaterial(), Vector3.forward);
        bridgeArrow.SetActive(false);
    }

    internal void SetBridgeArrowVisible(bool visible)
    {
        if (bridgeArrow != null)
            bridgeArrow.SetActive(visible);
    }

    private Material MarkerMaterial()
    {
        if (markerMaterial != null)
            return markerMaterial;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("Turntable marker shader is unavailable.");
        markerMaterial = new Material(shader);
        if (markerMaterial.HasProperty("_BaseColor"))
            markerMaterial.SetColor("_BaseColor", Color.white);
        return markerMaterial;
    }

    internal void Shutdown()
    {
        if (StateManager.IsHost && (goal != null || pendingIndex.HasValue))
        {
            Cancel(resetLever: true);
            if (controller != null)
                NativeAccess.StopMotor(controller);
        }
        initialized = false;
        targetObserver?.Dispose();
        leverObserver?.Dispose();
        targetObserver = null;
        leverObserver = null;
        foreach (var marker in markers.Values)
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
                Object.Destroy(marker.gameObject);
            }
        markers.Clear();
        if (bridgeArrow != null)
        {
            bridgeArrow.SetActive(false);
            Object.Destroy(bridgeArrow);
        }
        bridgeArrow = null;
        if (markerMaterial != null)
            Object.Destroy(markerMaterial);
        markerMaterial = null;
    }

    private void OnDestroy() => Shutdown();
}
