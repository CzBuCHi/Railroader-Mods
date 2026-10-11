using UnityEngine;

namespace TurntableControl;

internal static class TurntableArrow
{
    internal static LineRenderer Create(GameObject owner, Material material, Vector3 outward, bool receiver = false)
    {
        var arrow = owner.AddComponent<LineRenderer>();
        arrow.sharedMaterial = material;
        arrow.startColor = arrow.endColor = new Color(0.95f, 0.8f, 0.3f);
        arrow.startWidth = arrow.endWidth = 0.045f;
        arrow.positionCount = 5;
        arrow.useWorldSpace = false;
        arrow.loop = false;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.forward;
        var sideways = Vector3.Cross(Vector3.up, outward);
        if (receiver)
        {
            // The track chevron follows the bridge arrow with a clear gap when aligned.
            var notch = outward * 0.06f;
            var shoulder = outward * -0.24f;
            arrow.SetPosition(0, shoulder + sideways * 0.2f);
            arrow.SetPosition(1, notch);
            arrow.SetPosition(2, outward * 0.6f);
            arrow.SetPosition(3, notch);
            arrow.SetPosition(4, shoulder - sideways * 0.2f);
            return arrow;
        }
        var tip = outward * 0.45f;
        var headBase = outward * 0.15f;
        arrow.SetPosition(0, outward * -0.35f);
        arrow.SetPosition(1, tip);
        arrow.SetPosition(2, headBase + sideways * 0.2f);
        arrow.SetPosition(3, tip);
        arrow.SetPosition(4, headBase - sideways * 0.2f);
        return arrow;
    }
}