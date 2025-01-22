using Helpers;
using JetBrains.Annotations;
using MapEditor.MapState.TrackNodeEditor;
using MapEditor.MapState.TrackSegmentEditor;
using MapEditor.Utility;
using Track;
using UnityEngine;

namespace MapEditor.Visualizers;

[PublicAPI]
internal sealed class TrackNodeVisualizer : MonoBehaviour, IPickable
{
    #region Manage

    public static void CreateVisualizers() {
        foreach (var trackNode in Graph.Shared.Nodes) {
            CreateVisualizer(trackNode);
        }
    }

    public static void DestroyVisualizers() {
        foreach (var visualizer in Graph.Shared.GetComponentsInChildren<TrackNodeVisualizer>()!) {
            Destroy(visualizer.gameObject);
        }
    }

    public static void CreateVisualizer(TrackNode node) {
        if (node.GetComponentInChildren<TrackNodeVisualizer>() != null) {
            return;
        }

        var go = new GameObject("TrackNodeVisualizer");
        go.transform.SetParent(node.transform);
        go.AddComponent<TrackNodeVisualizer>();
    }

    #endregion

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    private TrackNode _TrackNode = null!;

    private LineRenderer? _LineRenderer;

    public void Awake() {
        _TrackNode = transform.parent.GetComponent<TrackNode>()!;

        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;

        gameObject.layer = Layers.Clickable;

        _LineRenderer = gameObject.AddComponent<LineRenderer>();
        _LineRenderer.material = _LineMaterial;
        _LineRenderer.startWidth = 0.05f;
        _LineRenderer.positionCount = 5;
        _LineRenderer.useWorldSpace = false;

        const float sizeX = -0.2f;
        const float sizeY = -0.4f;
        const float sizeZ = 0.3f;

        _LineRenderer.SetPosition(0, new Vector3(-sizeX, 0, sizeY));
        _LineRenderer.SetPosition(1, new Vector3(0, 0, sizeZ));
        _LineRenderer.SetPosition(2, new Vector3(sizeX, 0, sizeY));
        _LineRenderer.SetPosition(3, new Vector3(0, 0, -sizeZ));
        _LineRenderer.SetPosition(4, new Vector3(-sizeX, 0, sizeY));

        var boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);
    }

    public void Update() => _LineRenderer!.material.color = EditorState.IsTrackNodeSelected(_TrackNode) ? Color.magenta : Color.cyan;

    #region IPickable

    public void Activate(PickableActivateEvent evt) {
        var selectedTrackNode = EditorState.TrackNode;
        if (selectedTrackNode != null && InputHelper.GetShift()) {
            TrackSegmentUtility.CreateBetween(selectedTrackNode, _TrackNode);
        } else if (InputHelper.GetControl()) {
            EditorState.AddToSelection(_TrackNode);
        } else {
            EditorState.ReplaceSelection(_TrackNode);
        }
    }

    public void Deactivate() {
    }

    public float                    MaxPickDistance  => EditorState.SelectedPatch != null ? 200f : 0f;
    public int                      Priority         => 1;
    public TooltipInfo              TooltipInfo      => EditorState.SelectedPatch != null ? new TooltipInfo($"Node {_TrackNode.id}", string.Empty) : TooltipInfo.Empty;
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    #endregion
}
