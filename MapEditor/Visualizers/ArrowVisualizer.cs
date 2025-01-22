using UnityEngine;

namespace MapEditor.Visualizers;

internal class ArrowVisualizer : MonoBehaviour
{
    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    protected float VerticalOffset = 0;

    protected LineRenderer LineRenderer = null!;

    public virtual void Awake() {
        LineRenderer = CreateLineRenderer();
    }

    private LineRenderer CreateLineRenderer() {
        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = _LineMaterial;
        lineRenderer.material.color = Color.yellow;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.positionCount = 5;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, new Vector3(-0.2f, VerticalOffset + 0.5f, 0));
        lineRenderer.SetPosition(1, new Vector3(0, VerticalOffset, 0));
        lineRenderer.SetPosition(2, new Vector3(0, VerticalOffset + 5f, 0));
        lineRenderer.SetPosition(3, new Vector3(0, VerticalOffset, 0));
        lineRenderer.SetPosition(4, new Vector3(0.2f, VerticalOffset + 0.5f, 0));
        lineRenderer.enabled = true;
        return lineRenderer;
    }
}
