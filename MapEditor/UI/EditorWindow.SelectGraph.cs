using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapEditor.MapState;
using Serilog;
using UI.Builder;

namespace MapEditor.UI;

public sealed partial class EditorWindow
{
    private readonly Dictionary<string, List<string>> _Graphs =
        MapEditorPlugin.Shared!.Context.GetMixintos("game-graph")
                       .GroupBy(o => o.Source.ToString(), o => o.Mixinto)
                       .ToDictionary(o => o.Key, o => o.ToList());

    private List<string>? _Mods;
    private List<string>  Mods => _Mods ??= ["Select ...", .._Graphs.Keys];

    private int _ModIndex;
    private int _GraphIndex;

    private List<string> GetModGraphs() => _Graphs[Mods[_ModIndex]]!;

    private void BuildModSelector(UIPanelBuilder builder) {
        builder
            .AddField("Mod",
                builder.AddDropdown(Mods, _ModIndex, o => {
                    Log.Information("Mod changed: " + o);
                    _ModIndex = o;
                    _GraphIndex = 0;

                    if (_ModIndex == 0) {
                        UpdateState(null);
                    } else {
                        var modGraphs = GetModGraphs();
                        UpdateState(modGraphs[0], modGraphs.Count > 1);
                    }

                    builder.Rebuild();
                })
            )
            .Disable(MapStateEditor.Count > 0);

        if (_ModIndex == 0) {
            return;
        }

        var modGraphs = GetModGraphs();
        if (modGraphs.Count > 1) {
            builder
                .AddField("Graph",
                    builder.AddDropdown(modGraphs.Select(Path.GetFileNameWithoutExtension).ToList(), _GraphIndex, o => {
                        Log.Information("Graph changed: " + o);
                        _GraphIndex = o;
                        UpdateState(modGraphs[_GraphIndex], true);
                        builder.Rebuild();
                    })
                )
                .Disable(MapStateEditor.Count > 0);
        }

        return;

        void UpdateState(string? selectedPatch, bool modHasMultiplePatches = false) {
            EditorState.Update(state => state with {
                SelectedPatch = selectedPatch,
                ModHasMultiplePatches = modHasMultiplePatches
            });
        }
    }
}
