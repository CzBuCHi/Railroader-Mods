using MapEditor.Features.TrackNodes;
using MapEditor.MapState;
using MapMod.Loaders;
using Track;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Features.Loaders;

public static class LoaderEditor
{
    public static void Build(UIPanelBuilder builder, Loader loader) {
        builder.AddField("Identifier", builder.AddInputField(loader.Identifier, _ => { }));
        builder.AddField("Position", builder.AddInputField(loader.transform.localPosition.ToString(), _ => { }));

        builder.AddField("Flip loader orientation",
            builder.AddToggle(() => loader.FlipOrientation, val => {
                MapStateEditor.NextStep(new LoaderUpdate(loader.Identifier) { FlipOrientation = val });
            })!
        );

        var prefabIndex = LoaderUtility.GetPrefabIndex(loader);
        builder.AddField("Prefab",
            builder.AddDropdown(LoaderUtility.Prefabs, prefabIndex, index => {
                LoaderUtility.UpdatePrefab(loader, index);
                builder.Rebuild();
            })
        );

        if (prefabIndex < 3) { 
            // water tower and water column do not need an industry
            builder.AddField("Industry",
                builder.AddDropdown(LoaderUtility.Industries, LoaderUtility.GetIndustryIndex(loader), index => LoaderUtility.UpdateIndustry(loader, index))
            );
        }

        
        builder.AddSection("Operations", section => {
            section.ButtonStrip(strip => {
                strip.AddButton("Show", () => LoaderUtility.Show(loader));
                strip.AddButton("Remove", () => LoaderUtility.Remove(loader));
                strip.AddButton("Move", () => LoaderUtility.Move(loader));
            });
        });
    }
}
