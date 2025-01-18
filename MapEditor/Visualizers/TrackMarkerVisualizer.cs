using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Helpers;
using JetBrains.Annotations;
using Track;
using UnityEngine;

namespace MapEditor.Visualizers;

[PublicAPI]
internal sealed class TrackMarkerVisualizer : MonoBehaviour, IPickable
{
    #region Manage

    public static void CreateVisualizers() {
        var field        = typeof(Graph).GetField("_trackMarkers", BindingFlags.Instance | BindingFlags.NonPublic);
        var trackMarkers = (Dictionary<string, HashSet<TrackMarker>>)field.GetValue(Graph.Shared);

        foreach (var marker in trackMarkers.SelectMany(o=>o.Value)) {
            CreateVisualizer(marker);
        }
    }

    public static void DestroyVisualizers() {
        foreach (var visualizer in Graph.Shared.GetComponentsInChildren<TrackMarkerVisualizer>()!) {
            Destroy(visualizer.gameObject);
        }
    }

    public static void CreateVisualizer(TrackMarker node) {
        if (node.GetComponentInChildren<TrackMarkerVisualizer>() != null) {
            return;
        }

        var go = new GameObject("TrackMarkerVisualizer");
        go.transform.SetParent(node.transform);
        go.AddComponent<TrackMarkerVisualizer>();
    }

    #endregion

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    private TrackMarker _TrackMarker = null!;

    private LineRenderer? _LineRenderer;

    private TooltipInfo BuildTooltipInfo() {
        var sb = new StringBuilder();
        sb.AppendLine($"ID: {_TrackMarker.id}");
        sb.AppendLine($"Pos: {_TrackMarker.transform.localPosition}");
        sb.AppendLine($"Rot: {_TrackMarker.transform.localEulerAngles}");
        return new TooltipInfo($"Marker {_TrackMarker.id}", sb.ToString());
    }

    public void Awake() {
        _TrackMarker = transform.parent.GetComponent<TrackMarker>()!;

        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;

        gameObject.layer = Layers.Clickable;

        _LineRenderer = gameObject.AddComponent<LineRenderer>();
        _LineRenderer.material = _LineMaterial;
        _LineRenderer.startWidth = 0.05f;
        _LineRenderer.positionCount = 4;
        _LineRenderer.useWorldSpace = false;
        _LineRenderer.loop = true;

        const float size = 0.1f;

        _LineRenderer.SetPosition(0, new Vector3(-size, 0, 0));
        _LineRenderer.SetPosition(1, new Vector3(0, 0, -size));
        _LineRenderer.SetPosition(2, new Vector3(size, 0, 0));
        _LineRenderer.SetPosition(3, new Vector3(0, 0, size));
        
        var boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(size, 0.1f, size);
    }

    public void Update() {
        _LineRenderer!.material.color = Color.red;
    }

    #region IPickable

    public void Activate(PickableActivateEvent evt) {
    }

    public void Deactivate() {
    }

    public float                    MaxPickDistance  => EditorState.SelectedPatch != null ? 200f : 0f;
    public int                      Priority         => 1;
    public TooltipInfo              TooltipInfo      => EditorState.SelectedPatch != null ? BuildTooltipInfo() : TooltipInfo.Empty;
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    #endregion
}
