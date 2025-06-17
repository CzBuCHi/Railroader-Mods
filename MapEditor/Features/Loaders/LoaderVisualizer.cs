using System.Text;
using Helpers;
using JetBrains.Annotations;
using MapMod.Loaders;
using RLD;
using Track;
using UnityEngine;

namespace MapEditor.Features.Loaders;

public sealed class LoaderVisualizer : MonoBehaviour, IPickable
{
    #region Manage

    public static void CreateVisualizers() {
        foreach (var loader in FindObjectsByType<Loader>(FindObjectsInactive.Include, FindObjectsSortMode.None)!) {
            CreateVisualizer(loader);
        }
    }

    public static void DestroyVisualizers() {
        foreach (var visualizer in Graph.Shared.GetComponentsInChildren<LoaderVisualizer>()!) {
            Destroy(visualizer.gameObject);
        }
    }

    public static void CreateVisualizer(Loader loader) {
        if (loader.GetComponentInChildren<LoaderVisualizer>() != null) {
            return;
        }

        var go = new GameObject("LoaderVisualizer");
        go.transform.SetParent(loader.transform);
        go.AddComponent<LoaderVisualizer>();
    }

    #endregion

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    private Loader _Loader = null!;

    private LineRenderer? _LineRenderer;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public TooltipInfo              TooltipInfo      => BuildTooltipInfo();
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

    private TooltipInfo BuildTooltipInfo() {
        var sb = new StringBuilder();
        sb.AppendLine($"ID: {_Loader.Identifier}");
        sb.AppendLine($"Loc: {_Loader.Location}");
        sb.AppendLine($"Prefab: {_Loader.Prefab}");
        sb.AppendLine($"Industry: {_Loader.Industry}");

        return new TooltipInfo($"Loader {_Loader.Identifier}", sb.ToString());
    }

    [UsedImplicitly]
    public void Start() {
        _Loader = transform.parent.GetComponent<Loader>()!;

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

    [UsedImplicitly]
    public void Update() {
        _LineRenderer!.material.color = EditorState.Loader?.Identifier == _Loader.Identifier ? Color.magenta : Color.cyan;
    }

    public void Activate(PickableActivateEvent evt) {
        EditorState.ReplaceSelection(_Loader);
    }

    public void Deactivate() {
    }
}
