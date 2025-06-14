using System.Linq;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Utility;
using MapEditor.Visualizers;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

[PublicAPI]
internal sealed class TelegraphPoleVisualizer : ArrowVisualizer, IPickable
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

    public override void Awake() {
        base.Awake();
        VerticalOffset = 10f;

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

        LineRenderer.enabled = EditorState.SelectedAssets.OfType<TelegraphPoleId>().Select(o => o.Id).Contains(NodeId);
    }
}
