using System;
using System.IO;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using MapEditor.Behaviours;
using MapEditor.Events;
using MapEditor.Utility;
using MapEditor.Visualizers;
using Serilog;
using StrangeCustoms.Tracks;
using Track;

namespace MapEditor;

public static class EditorState
{
    private static EditorStateData _Instance = new();

    public static bool    ModHasMultiplePatches => _Instance.ModHasMultiplePatches;
    public static string? SelectedPatch         => _Instance.SelectedPatch;
    public static object? SelectedAsset         => _Instance.SelectedAsset;

    public static TrackNode?       TrackNode     => SelectedAsset as TrackNode;
    public static TrackSegment?    TrackSegment  => SelectedAsset as TrackSegment;
    public static TelegraphPoleId? TelegraphPole => SelectedAsset as TelegraphPoleId;

    public static void Reset() => Update(_ => new EditorStateData());

    public static void Update(Func<EditorStateData, EditorStateData> update) {
        var newState = update(_Instance)!;
        if (_Instance == newState) {
            return;
        }

        var oldState = _Instance;
        _Instance = newState;
        UnityHelpers.CallOnNextFrame(() => OnChanged(oldState));
    }

    private static void OnChanged(EditorStateData oldState) {
#if DEBUG
        global::UI.Console.Console.shared.AddLine($"State: {_Instance}");
        Log.Information($"State: {_Instance}");
#endif
        if (oldState.SelectedPatch != _Instance.SelectedPatch) {
            if (_Instance.SelectedPatch != null) {
                OnEditorEnabled();
            } else {
                OnEditorDisabled();
            }
        }

        if (oldState.SelectedAsset != _Instance.SelectedAsset) {
            OnSelectedAssetChanged(oldState.SelectedAsset, _Instance.SelectedAsset);
        }

        Messenger.Default.Send(new MapEditorStateChanged());
    }

    private static void OnSelectedAssetChanged(object? oldSelectedAsset, object? newSelectedAsset) {
        Log.Information($"SelectedAsset: {Info(oldSelectedAsset)} => {Info(newSelectedAsset)}");

        MoveableObject.Destroy();

        switch (oldSelectedAsset) {
            case TrackNode trackNode:
                TrackSegmentVisualizer.HideVisualizers(trackNode);
                break;

            case TrackSegment trackSegment:
                TrackSegmentVisualizer.HideVisualizer(trackSegment);
                break;
        }

        switch (newSelectedAsset) {
            case TrackNode trackNode:
                TrackSegmentVisualizer.ShowVisualizers(trackNode);
                break;

            case TrackSegment trackSegment:
                TrackSegmentVisualizer.ShowVisualizer(trackSegment);
                break;
        }

        return;

        string Info(object? value) => value?.GetType().Name ?? "NULL";
    }

    private static void OnEditorEnabled() {
        MapEditorPlugin.PatchEditor = new PatchEditor(SelectedPatch!);
        TrackNodeVisualizer.CreateVisualizers();
        TrackMarkerVisualizer.CreateVisualizers();
        TrackSegmentVisualizer.CreateVisualizers();
    }

    private static void OnEditorDisabled() {
        MapEditorPlugin.PatchEditor = null;
        TrackNodeVisualizer.DestroyVisualizers();
        TrackMarkerVisualizer.DestroyVisualizers();
        TrackSegmentVisualizer.DestroyVisualizers();
    }
}

public record EditorStateData
{
    public bool    ModHasMultiplePatches { get; init; }
    public string? SelectedPatch         { get; init; }
    public object? SelectedAsset         { get; init; }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("EditorState {");
        sb.Append("SelectedPatch = ");
        if (SelectedPatch != null) {
            sb.Append(Path.GetFileName(Path.GetDirectoryName(SelectedPatch)!)).Append("\\").Append(Path.GetFileName(SelectedPatch));
        }

        sb.Append(", SelectedAsset = ");
        if (SelectedAsset != null) {
            sb.Append(SelectedAsset);
        }

        sb.Append(" }");
        return sb.ToString();
    }
}

public record TelegraphPoleId(int Id);
