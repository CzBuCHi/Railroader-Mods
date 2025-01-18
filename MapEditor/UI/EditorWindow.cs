using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Events;
using MapEditor.Extensions;
using MapEditor.Harmony;
using MapEditor.MapState;
using MapEditor.Utility;
using MapEditor.Visualizers;
using Newtonsoft.Json.Linq;
using StrangeCustoms.Tracks;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.UI;

[PublicAPI]
public sealed partial class EditorWindow : MonoBehaviour, IProgrammaticWindow
{
    public UIBuilderAssets BuilderAssets    { get; set; } = null!;
    public string          WindowIdentifier { get; }      = "EditorWindow";
    public Vector2Int      DefaultSize      { get; }      = new(400, 300);
    public Window.Position DefaultPosition  { get; }      = Window.Position.UpperLeft;
    public Window.Sizing   Sizing           { get; }      = Window.Sizing.Resizable(new(200, 200));

    public static EditorWindow Shared => WindowManager.Shared!.GetWindow<EditorWindow>()!;

    private Window   _Window = null!;
    private UIPanel? _Panel;

    public void Awake() {
        _Window = GetComponent<Window>()!;
    }

    public void Show() {
        Populate();
        SetPosition();
        _Window.ShowWindow();
    }

    public void SetPosition() {
        var rectTransform = _Window.GetComponent<RectTransform>()!;
        rectTransform.position = new Vector2(Screen.width, Screen.height - 30).Round();
    }

    public static void Toggle() {
        if (Shared._Window.IsShown) {
            Shared._Window.CloseWindow();
            EditorState.Reset();
        } else {
            Shared.Show();
        }
    }

    public void OnDisable() {
        _Panel?.Dispose();
        _Panel = null;
    }

    private void Populate() {
        _Window.Title = "Map Editor";
        _Panel?.Dispose();
        _Panel = UIPanel.Create(_Window.contentRectTransform!, BuilderAssets, Build);
    }

    private void Build(UIPanelBuilder builder) {
        builder.RebuildOnEvent<MapEditorStateChanged>();

        BuildModSelector(builder);

        if (EditorState.SelectedPatch != null) {
            BuildEditor(builder);
        }

        if (EditorState.SelectedAsset != null) {
            builder.AddSection(
                EditorState.SelectedAsset.ToString(),
                section => BuildAssetEditor(section, EditorState.SelectedAsset)
            );
        } else if (EditorState.SelectedPatch != null) {
            BuildEmptyEditor(builder);
        }

        builder.AddExpandingVerticalSpacer();
    }

    private void BuildEditor(UIPanelBuilder builder) {
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

    private void BuildAssetEditor(UIPanelBuilder builder, object selectedAsset) {
        builder.AddButton("Unselect", () => EditorState.Update(state => state with { SelectedAsset = null }));

        switch (selectedAsset) {
            case TrackNode trackNode:
                BuildTrackNodeEditor(builder, trackNode);
                break;
            case TrackSegment trackSegment:
                BuildTrackSegmentEditor(builder, trackSegment);
                break;
            case TelegraphPoleId telegraphPoleId:
                BuildTelegraphPoleEditor(builder, telegraphPoleId.Id);
                break;
            case SceneryAssetInstance sceneryAssetInstance:
                BuildSceneryAssetInstanceEditor(builder, sceneryAssetInstance);
                break;
        }
    }

    private Camera _MainCamera = null!;

    private void BuildEmptyEditor(UIPanelBuilder builder) {
        if (!MainCameraHelper.TryGetIfNeeded(ref _MainCamera)) {
            return;
        }

        if (!WorldTransformer.TryGetShared(out var worldTransformer) || worldTransformer == null) {
            builder.AddLabel("Error: Cannot find World");
            return;
        }

        builder.ButtonStrip(strip => {
            strip.AddButton("Scene Viewer", SceneWindow.Toggle);
            strip.AddButton("Place object", PlaceObject);
        });

        return;

        void PlaceObject() {
            Toast.Present("Click on ground to continue ...");
            UnityHelpers.CallOnceOnMouseButton(0, () => {
                if (!UnityHelpers.RayPointFromMouse(_MainCamera, out var point)) {
                    return;
                }

                SceneWindow.SelectTemplate(template => {
                    template.SetActive(false);

                    var go = Instantiate(template);
                    template.SetActive(true);

                    go.transform.SetParent(template.transform.parent, true);
                    go.transform.position = point;
                    go.name = IdGenerators.Scenery.Next();
                    go.SetActive(true);

                    var sceneryAssetInstance = go.GetComponent<SceneryAssetInstance>()!;
                    EditorState.Update(state => state with { SelectedAsset = sceneryAssetInstance });
                    MapEditorPlugin.PatchEditor.AddOrUpdateScenery(sceneryAssetInstance.name, new SerializedScenery(sceneryAssetInstance));
                });

            });
        }
    }

    private record EditorWindowChanged;

}
