using Helpers;
using Track;
using UnityEngine;

namespace TurntableControl;

internal sealed class TurntableMarker : MonoBehaviour, IPickable
{
    private static TurntableMarker? hovered;
    private TurntableDriver? driver;
    private LineRenderer? arrow;
    private int index;
    internal TrackNode Node { get; private set; } = null!;

    public float MaxPickDistance => 200f;
    public int Priority => 0;
    public TooltipInfo TooltipInfo => new TooltipInfo("Turntable track " + (index + 1),
        "Click: rotate at full power. Shift + click: stop.");
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

    internal void Initialize(TurntableDriver owner, TrackNode node, int trackIndex, Material material)
    {
        driver = owner;
        Node = node;
        index = trackIndex;
        gameObject.layer = Layers.Clickable;
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(1f, 0.5f, 1f);

        // Compute the outward direction explicitly; node rotation/name is irrelevant.
        arrow = TurntableArrow.Create(gameObject, material, -transform.InverseTransformPoint(owner.Center), receiver: true);
        arrow.enabled = false;
    }

    internal static void SetHovered(TurntableMarker? next)
    {
        if (hovered == next)
            return;
        if (hovered != null)
        {
            if (hovered.arrow != null)
                hovered.arrow.enabled = false;
            hovered.driver?.SetBridgeArrowVisible(false);
        }
        hovered = next;
        if (hovered != null)
        {
            if (hovered.arrow != null)
                hovered.arrow.enabled = true;
            hovered.driver?.SetBridgeArrowVisible(true);
        }
    }

    private void OnDisable()
    {
        if (hovered == this)
            SetHovered(null);
        if (arrow != null)
            arrow.enabled = false;
    }

    public void Activate(PickableActivateEvent evt)
    {
        driver?.RequestMove(index, evt.IsShiftDown);
    }

    public void Deactivate() { }
}
