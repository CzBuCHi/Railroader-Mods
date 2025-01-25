using System;
using System.Collections.Generic;
using System.Linq;
using CzBuCHi.Shared.UI;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Behaviours;
using MapEditor.Events;
using MapEditor.MapState;
using MapEditor.MapState.TrackNodeEditor;
using MapEditor.UI.Controls;
using MapEditor.Visualizers;
using Serilog;
using Track;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.UI;

[PublicAPI]
public sealed class EditorWindow : ProgrammaticWindowBase
{
    public override Window.Position DefaultPosition { get; } = Window.Position.UpperLeft;
    public override Window.Sizing   Sizing          { get; } = Window.Sizing.Resizable(new Vector2Int(400, 300));

    public static EditorWindow Shared => WindowManager.Shared!.GetWindow<EditorWindow>()!;

    protected override void WindowOnOnShownDidChange(bool isShown) {
        base.WindowOnOnShownDidChange(isShown);
        if (isShown) {
            var rectTransform = Window.GetComponent<RectTransform>()!;
            rectTransform.position = new Vector2(Screen.width, Screen.height - 30).Round();
        } else {
            EditorState.Reset();
        }
    }

    public static void Toggle() {
        if (Shared.Window.IsShown) {
            Shared.Window.CloseWindow();
        } else {
            Shared.ShowWindow();
        }
    }

    public override void OnDisable() {
        base.OnDisable();
        EditorState.Reset();
    }

    private UIState<string?> _SelectedItem = new(null);

    protected override void Build(UIPanelBuilder builder) {
        builder.RebuildOnEvent<MapEditorStateChanged>();
        SelectGraph.Build(builder);

        if (EditorState.SelectedPatch != null) {
            MainEditor.Build(builder);
        }

        if (EditorState.SelectedAssets != null) {
            AssetsEditor.Build(builder, EditorState.SelectedAssets);

            var data = EditorState.SelectedAssets.Select(o => new UIPanelBuilder.ListItem<object>(o.ToString(), o, "", o.GetType().Name)).ToList();
            builder.AddListDetail(data, _SelectedItem, BuildAssetEditor);
        } else if (EditorState.SelectedPatch != null) {
            EmptyEditor.Build(builder);
        }

        builder.AddExpandingVerticalSpacer();
    }

    private void BuildAssetEditor(UIPanelBuilder builder, object selectedAsset) {
        builder.AddButton("Clear selection", EditorState.ClearSelection);

        switch (selectedAsset) {
            case TrackNode trackNode:
                TrackNodeEditor.Build(builder, trackNode);
                break;
            case TrackSegment trackSegment:
                TrackSegmentEditor.Build(builder, trackSegment);
                break;
            case TelegraphPoleId telegraphPoleId:
                TelegraphPoleEditor.Build(builder, telegraphPoleId.Id);
                break;
            case SceneryAssetInstance sceneryAssetInstance:
                SceneryAssetInstanceEditor.Build(builder, sceneryAssetInstance);
                break;
        }
    }
}
