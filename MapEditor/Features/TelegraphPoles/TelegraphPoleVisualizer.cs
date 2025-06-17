using System.Linq;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Utility;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

[PublicAPI]
internal sealed class TelegraphPoleVisualizer : MonoBehaviour, IPickable
{
    #region IPickable

    public void Activate(PickableActivateEvent evt) {
        if (InputHelper.GetControl()) {
            EditorState.AddToSelection(new TelegraphPoleId(NodeId));
        } else {
            EditorState.ReplaceSelection(new TelegraphPoleId(NodeId));
        }
    }

    public void Deactivate() {
    }

    public float                    MaxPickDistance  => EditorState.SelectedPatch != null ? 200f : 0f;
    public int                      Priority         => 1;
    public TooltipInfo              TooltipInfo      => new("Telegraph Pole", string.Empty);
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    #endregion

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    private LineRenderer _LineRenderer   = null!;
    private float        _VerticalOffset = 10f;

    private LineRenderer CreateLineRenderer() {
        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = _LineMaterial;
        lineRenderer.material.color = Color.yellow;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.positionCount = 5;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, new Vector3(-0.2f, _VerticalOffset + 0.5f, 0));
        lineRenderer.SetPosition(1, new Vector3(0, _VerticalOffset, 0));
        lineRenderer.SetPosition(2, new Vector3(0, _VerticalOffset + 5f, 0));
        lineRenderer.SetPosition(3, new Vector3(0, _VerticalOffset, 0));
        lineRenderer.SetPosition(4, new Vector3(0.2f, _VerticalOffset + 0.5f, 0));
        lineRenderer.enabled = true;
        return lineRenderer;
    }

    public void Awake() {
        _LineRenderer = CreateLineRenderer();

        gameObject.layer = Layers.Clickable;

        var boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 8.9f, 0);
        boxCollider.size = Vector3.one;
    }

    public int NodeId;

    public void Update() {
        if (EditorState.SelectedAssets == null) {
            return;
        }

        _LineRenderer.enabled = EditorState.SelectedAssets.OfType<TelegraphPoleId>().Select(o => o.Id).Contains(NodeId);
    }
}
