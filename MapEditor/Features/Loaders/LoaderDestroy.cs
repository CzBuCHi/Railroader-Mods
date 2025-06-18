using MapEditor.MapState;
using MapEditor.Utility;
using MapMod.Loaders;
using Track;

namespace MapEditor.Features.Loaders;

public sealed record LoaderDestroy(string Identifier) : IStateStep
{
    private LoaderData? _Data;

    public void Do() {
        var loader = Loader.FindById(Identifier);
        if (loader == null) {
            return;
        }

        EditorState.RemoveFromSelection(loader);
        _Data = LoaderUtility.Destroy(loader);
    }

    public void Undo() {
        if (_Data == null) {
            return;
        }

        var loader = LoaderUtility.Create(Identifier, _Data);
        UnityHelpers.CallOnNextFrame(() => LoaderVisualizer.CreateVisualizer(loader));
    }

#if DEBUG
    public string DoText   => $"LoaderDestroy = {{ Identifier = {Identifier} }}";
    public string UndoText => $"LoaderCreate = {{ Identifier = {Identifier} }}";
#endif
}
