using MapEditor.Extensions;
using MapEditor.MapState;
using MapMod.Loaders;
using Track;

namespace MapEditor.Features.Loaders;

public class LoaderUpdate(string identifier) : IStateStep
{
    public Location? OriginalLocation        { get; init; }
    public bool?     OriginalFlipOrientation { get; init; }
    public string?   OriginalPrefab          { get; init; }
    public string?   OriginalIndustry        { get; init; }

    public Location? Location        { get; init; }
    public bool?     FlipOrientation { get; init; }
    public string?   Prefab          { get; init; }
    public string?   Industry        { get; init; }

    public void Do() {
        if (Location == null &&
            FlipOrientation == null &&
            Prefab == null &&
            Industry == null) {
            return;
        }

        var loader = Loader.FindById(identifier);
        if (loader == null) {
            return;
        }

        if (Location != null) {
            loader.Location = Location.Value;
        }

        if (FlipOrientation != null) {
            loader.FlipOrientation = FlipOrientation.Value;
        }

        if (Prefab != null) {
            loader.Prefab = Prefab;
        }

        if (Industry != null) {
            loader.Industry = Industry;
        }

        MapEditorPlugin.PatchEditor!.AddOrUpdateLoader(loader);
    }

    public void Undo() {
        if (OriginalLocation == null &&
            OriginalFlipOrientation == null &&
            OriginalPrefab == null &&
            OriginalIndustry == null) {
            return;
        }

        var loader = Loader.FindById(identifier);
        if (loader == null) {
            return;
        }

        if (OriginalLocation != null) {
            loader.Location = OriginalLocation.Value;
        }

        if (OriginalFlipOrientation != null) {
            loader.FlipOrientation = OriginalFlipOrientation.Value;
        }

        if (OriginalPrefab != null) {
            loader.Prefab = OriginalPrefab;
        }

        if (OriginalIndustry != null) {
            loader.Industry = OriginalIndustry;
        }

        MapEditorPlugin.PatchEditor!.AddOrUpdateLoader(loader);
    }

#if DEBUG
    public string DoText {
        get {
            var loader = Loader.FindById(identifier)!;
            return "LoaderUpdate { Id = " + identifier + ", " +
                   (Location != null ? $"Location = {loader.Location} -> {Location}, " : "") +
                   (FlipOrientation != null ? $"FlipOrientation = {loader.FlipOrientation} -> {FlipOrientation}, " : "") +
                   (Prefab != null ? $"Prefab = {loader.Prefab} -> {Prefab}, " : "") +
                   (Industry != null ? $"Industry = {loader.Industry} -> {Industry}, " : "") +
                   " }";
        }
    }

    public string UndoText {
        get {
            var loader = Loader.FindById(identifier)!;
            return "LoaderUpdate { Id = " + identifier + ", " +
                   (OriginalLocation != null ? $"Location = {loader.Location} -> {OriginalLocation}, " : "") +
                   (OriginalFlipOrientation != null ? $"FlipOrientation = {loader.FlipOrientation} -> {OriginalFlipOrientation}, " : "") +
                   (OriginalPrefab != null ? $"Prefab = {loader.Prefab} -> {OriginalPrefab}, " : "") +
                   (OriginalIndustry != null ? $"Industry = {loader.Industry} -> {OriginalIndustry}, " : "") +
                   " }";
        }
    }
#endif
}
