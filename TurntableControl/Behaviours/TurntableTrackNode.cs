using Helpers;
using JetBrains.Annotations;
using Track;
using UnityEngine;

namespace TurntableControl.Behaviours;

[PublicAPI]
public class TurntableTrackNode : MonoBehaviour, IPickable
{
    public  TurntableControllerEx Controller = null!;
    private int                   _TrackNodeIndex;

    public void Activate(PickableActivateEvent evt) {
        Controller.MoveToIndex(_TrackNodeIndex);
    }

    public void Deactivate() {
    }

    private LineRenderer? _LineRenderer;

    public float                    MaxPickDistance  => 200;
    public int                      Priority         => 0;
    public TooltipInfo              TooltipInfo      => TooltipInfo.Empty;
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    private static readonly Material _LineMaterial = new(Shader.Find("Universal Render Pipeline/Lit")!);

    public void Awake() {
        gameObject!.layer = Layers.Clickable;

        var trackNodeName = GetComponent<TrackNode>()!.name!;
        var index         = trackNodeName.LastIndexOf(' ');
        _TrackNodeIndex = int.Parse(trackNodeName.Substring(index + 1));

        var boxCollider = gameObject.AddComponent<BoxCollider>()!;
        boxCollider.center = Vector3.zero;
        boxCollider.size = Vector3.one;

        _LineRenderer = gameObject.AddComponent<LineRenderer>()!;
        _LineRenderer.material = _LineMaterial;
        _LineRenderer.material.color = Color.yellow;
        _LineRenderer.startWidth = 0.05f;
        _LineRenderer.positionCount = 4;
        _LineRenderer.useWorldSpace = false;
        _LineRenderer.loop = true;

        const float size = 0.1f;

        _LineRenderer.SetPosition(0, new Vector3(-size, 0, 0));
        _LineRenderer.SetPosition(1, new Vector3(0, 0, -size));
        _LineRenderer.SetPosition(2, new Vector3(size, 0, 0));
        _LineRenderer.SetPosition(3, new Vector3(0, 0, size));
    }
}
