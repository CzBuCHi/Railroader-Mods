using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using MapEditor.Behaviours;
using MapEditor.Events;
using MapEditor.Features.Loaders;
using MapEditor.Features.TrackNodes;
using MapEditor.Features.TrackSegments;
using MapEditor.Utility;
using MapMod.Loaders;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UnityEngine;

namespace MapEditor;

public static class EditorState
{
    private static EditorStateData _Instance = new();

    public static string? SelectedPatch  => _Instance.SelectedPatch;
    public static ImmutableArray? SelectedAssets => _Instance.SelectedAssets;

    public static bool IsTrackNodeSelected(TrackNode node) => IsEntity(node);
    public static bool IsTrackSegmentSelected(TrackSegment segment) => IsEntity(segment);
    public static bool IsTelegraphPoleSelected(int telegraphPoleId) => IsEntity(new TelegraphPoleId(telegraphPoleId));

    private static bool IsEntity(object entity) => SelectedAssets?.Contains(entity) == true;

    public static TrackNode? TrackNode => SelectedAssets is { Length: 1 } && SelectedAssets[0] is TrackNode trackNode ? trackNode : null;

    public static Loader? Loader => SelectedAssets is { Length: 1 } && SelectedAssets[0] is Loader loader ? loader : null;

    public static void AddToSelection(object entity) {
        Log.Information("AddToSelection: " + entity);
        Update(state => {
            var selectedAssets = SelectedAssets;
            if (selectedAssets == null) {
                selectedAssets = new ImmutableArray(entity);
            } else {
                if (selectedAssets.Contains(entity)) {
                    return state;
                }

                selectedAssets = selectedAssets.Add(entity);
            }

            return state with { SelectedAssets = selectedAssets };
        });
    }

    public static void RemoveFromSelection(object entity) {
        Log.Information("RemoveFromSelection: " + entity);
        Update(state => {
            var selectedAssets = SelectedAssets;
            if (selectedAssets == null) {
                return state;
            }

            if (!selectedAssets.Contains(entity)) {
                return state;
            }

            selectedAssets = selectedAssets.Remove(entity);

            return state with { SelectedAssets = selectedAssets };
        });
    }

    public static void ReplaceSelection(object entity) {
        Log.Information("ReplaceSelection: " + entity);
        Update(state => state with { SelectedAssets = new ImmutableArray(entity) });
    }

    public static void ClearSelection() {
        Log.Information("ClearSelection");
        Update(state => state with { SelectedAssets = null });
    }

    public static void Reset() => Update(_ => new EditorStateData());

    // private?
    public static void Update(Func<EditorStateData, EditorStateData> update) {
        Log.Information("Update");
        var newState = update(_Instance)!;
        if (_Instance == newState) {
            Log.Information("Update: no change");
            return;
        }

        Log.Information("Update: change");
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

        if (oldState.SelectedAssets != _Instance.SelectedAssets) {
            OnSelectedAssetChanged(oldState.SelectedAssets, _Instance.SelectedAssets);
        }

        Messenger.Default.Send(new MapEditorStateChanged());
    }

    private static void OnSelectedAssetChanged(ImmutableArray? oldSelectedAsset, ImmutableArray? newSelectedAsset) {
        Log.Information($"SelectedAsset: {oldSelectedAsset} => {newSelectedAsset}");

        MoveableObject.Destroy();

        if (oldSelectedAsset != null) {
            foreach (var item in oldSelectedAsset) {
                switch (item) {
                    case TrackNode trackNode:
                        TrackSegmentVisualizer.HideVisualizers(trackNode);
                        break;

                    case TrackSegment trackSegment:
                        TrackSegmentVisualizer.HideVisualizer(trackSegment);
                        break;
                }
            }
        }

        if (newSelectedAsset != null) {
            foreach (var item in newSelectedAsset) {
                switch (item) {
                    case TrackNode trackNode:
                        TrackSegmentVisualizer.ShowVisualizers(trackNode);
                        break;

                    case TrackSegment trackSegment:
                        TrackSegmentVisualizer.ShowVisualizer(trackSegment);
                        break;
                }
            }
        }
    }

    private static void OnEditorEnabled() {
        MapEditorPlugin.PatchEditor = new PatchEditor(SelectedPatch!);
        TrackNodeVisualizer.CreateVisualizers();
        TrackSegmentVisualizer.CreateVisualizers();
        LoaderVisualizer.CreateVisualizers();
    }

    private static void OnEditorDisabled() {
        MapEditorPlugin.PatchEditor = null;
        TrackNodeVisualizer.DestroyVisualizers();
        TrackSegmentVisualizer.DestroyVisualizers();
        LoaderVisualizer.DestroyVisualizers();
    }
}

public record EditorStateData
{
    public bool            ModHasMultiplePatches { get; init; }
    public string?         SelectedPatch         { get; init; }
    public ImmutableArray? SelectedAssets        { get; init; }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("EditorState {");
        sb.Append("SelectedPatch = ");
        if (SelectedPatch != null) {
            sb.Append(Path.GetFileName(Path.GetDirectoryName(SelectedPatch)!)).Append("\\").Append(Path.GetFileName(SelectedPatch));
        }

        sb.Append(", SelectedAssets = ");
        if (SelectedAssets != null) {
            sb.Append(SelectedAssets);
        } else {
            sb.Append("null");
        }

        sb.Append(" }");
        return sb.ToString();
    }
}

public record TelegraphPoleId(int Id);
