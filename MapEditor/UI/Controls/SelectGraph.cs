﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapEditor.MapState;
using Serilog;
using UI.Builder;
using UI.Common;
using UnityEngine.UI;

namespace MapEditor.UI.Controls;

public static class SelectGraph
{
    private static readonly Dictionary<string, List<string>> _Graphs =
        MapEditorPlugin.Shared!.Context.GetMixintos("game-graph")
                       .GroupBy(o => o.Source.ToString(), o => o.Mixinto)
                       .ToDictionary(o => o.Key, o => o.ToList());

    private static List<string>? _Mods;
    private static List<string>  Mods => _Mods ??= ["Select ...", .._Graphs.Keys];

    private static int _ModIndex;
    private static int _GraphIndex;

    private static List<string> GetModGraphs() => _Graphs[Mods[_ModIndex]]!;

    public static void Build(UIPanelBuilder builder) {
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

        builder.AddButton("Create new simple mod", CreateNewMod);

        if (_ModIndex == 0) {
            EditorState.Reset();
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

    private const string Readme =
        """
        This mod was generated by the map editor as a scaffold mod.
        
         Instructions  

        1. Rename the folder
           Rename the folder `MapEditor.SimpleMod` to your preferred name to better represent your mod.  

        2. Update the `id` field
           Replace the existing value with a unique identifier for your mod.  
           It’s recommended to use your nickname as a prefix for clarity and uniqueness.  
           Example: "id": "Nick.MyCoolMod"

        3. Update the `name` field
           Replace the existing value with a human-readable name for the mod.  
           Example: "name": "My Cool Mod"

        4. Update the `version` field
           Replace the existing value with your desired version number, following semantic versioning rules (e.g., `1.0.0`, `0.2.1`).  
           Example: "version": "1.0.0"

        5. Delete this file
           Once all changes are made, you can safely delete this file.  
        """;

    private const string DefinitionJson =
        """
        {
          "manifestVersion": 5,
          "id": "#id#",
          "name": "#id#",
          "version": "1.0",
          "requires": [
            "Zamu.StrangeCustoms",
          ],
          "mixintos": {
            "game-graph": "file(game-graph.json)"
          }
        }
        """;

    private static void CreateNewMod() {
        var basePath = Path.Combine(MapEditorPlugin.Shared!.Context.ModsBaseDirectory, "MapEditor.SimpleMod");

        var modPath = basePath;
        var i       = 0;
        while (Directory.Exists(modPath)) {
            modPath = $"{basePath}.{++i}";
        }

        var modName = Path.GetFileName(modPath);

        Directory.CreateDirectory(modPath);

        File.WriteAllText(Path.Combine(modPath, "Definition.json"), DefinitionJson.Replace("#id#", modName));
        File.WriteAllText(Path.Combine(modPath, "game-graph.json"), "{}");
        File.WriteAllText(Path.Combine(modPath, "README.txt"), Readme);

        Toast.Present($"Created new mod '{modName}'. Open README.txt file for more instructions.");
    }
}
