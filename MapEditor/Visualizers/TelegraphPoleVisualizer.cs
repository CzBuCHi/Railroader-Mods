using JetBrains.Annotations;

namespace MapEditor.Visualizers;

[PublicAPI]
internal sealed class TelegraphPoleVisualizer : ArrowVisualizer, IPickable
{
    #region IPickable

    public void Activate(PickableActivateEvent evt) {
        EditorState.Update(state => state with { SelectedAsset = new TelegraphPoleId(NodeId) });
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
    }

    public int NodeId;

    public void Update() {
        LineRenderer.enabled = EditorState.TelegraphPole?.Id == NodeId;
    }
}
