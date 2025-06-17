using MapMod.Loaders;
using StrangeCustoms.Tracks;

namespace MapEditor.Features.Loaders;

internal static class PatchEditorExtensions
{
    public static void AddOrUpdateLoader(this PatchEditor patchEditor, Loader loader) =>
        patchEditor.AddOrUpdateSpliney(loader.Identifier, _ => {
            var sl = new LoaderData();
            sl.Read(loader);
            return MapMod.Utility.Serialize(sl);
        });
}
