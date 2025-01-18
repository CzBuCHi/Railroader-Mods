using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace CzBuCHi.Shared.UI;

public abstract class ProgrammaticWindowBase : MonoBehaviour, IProgrammaticWindow
{
    public          UIBuilderAssets BuilderAssets    { get; set; } = null!;
    public          string          WindowIdentifier => GetType().FullName;
    public          Vector2Int      DefaultSize      => Sizing.MinSize;
    public virtual  Window.Position DefaultPosition  => Window.Position.Center;
    public abstract Window.Sizing   Sizing           { get; }
    protected       Window          Window           { get; private set; } = null!;
    private         UIPanel?        _Panel;

    public virtual void Awake() => Window = GetComponent<Window>()!;

    public virtual void OnDisable() {
        _Panel?.Dispose();
        _Panel = null;
    }

    public void ShowWindow() {
        Populate();
        Window.ShowWindow();
    }

    public void CloseWindow() {
        if (Window.IsShown) {
            Window.CloseWindow();
        }
    }

    private void Populate() {
        _Panel?.Dispose();
        _Panel = UIPanel.Create(Window.contentRectTransform!, BuilderAssets, Build);
    }

    protected abstract void Build(UIPanelBuilder builder);
}
