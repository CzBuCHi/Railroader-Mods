using MapEditor.Events;
using MapEditor.MapState;
using MapEditor.Utility;
using Track;
using UI.Builder;

namespace MapEditor.UI.Controls;

public static class MainEditor
{
    public static void Build(UIPanelBuilder builder) {
        builder.RebuildOnEvent<UndoRedoChanged>();

        builder.AddField("Prefix", builder.AddInputField(IdGenerators.Prefix, s => IdGenerators.Prefix = s));
        builder.AddField("Changes", MapStateEditor.Steps());

        builder.ButtonStrip(strip => {
            strip.AddButton("Undo All", MapStateEditor.UndoAll).Disable(!MapStateEditor.CanUndo);
            strip.AddButton("Undo", MapStateEditor.Undo).Disable(!MapStateEditor.CanUndo);
            strip.AddButton("Redo", MapStateEditor.Redo).Disable(!MapStateEditor.CanRedo);
            strip.AddButton("Redo All", MapStateEditor.RedoAll).Disable(!MapStateEditor.CanRedo);
        });

        builder.ButtonStrip(strip => {
            strip.AddButton("Rebuild Track", TrackObjectManager.Instance.Rebuild);
            strip.AddButton("Save changes", () => {
                MapEditorPlugin.PatchEditor!.Save();
                MapStateEditor.Clear();
            });
            strip.AddButton("Refresh", () => builder.Rebuild());
        });
    }
}
