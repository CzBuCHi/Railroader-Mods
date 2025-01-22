using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight.Messaging;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Behaviours;
using Serilog;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.UI;

[PublicAPI]
public sealed class SceneWindow : MonoBehaviour, IProgrammaticWindow
{
    public UIBuilderAssets BuilderAssets    { get; set; } = null!;
    public string          WindowIdentifier { get; }      = "SceneWindow";
    public Vector2Int      DefaultSize      { get; }      = new(1200, 600);
    public Window.Position DefaultPosition  { get; }      = Window.Position.LowerRight;
    public Window.Sizing   Sizing           { get; }      = Window.Sizing.Resizable(new(1200, 600));

    public static SceneWindow Shared => WindowManager.Shared!.GetWindow<SceneWindow>()!;

    private Window   _Window = null!;
    private UIPanel? _Panel;

    public void Awake() {
        _Window = GetComponent<Window>()!;
    }

    public void Show() {
        Populate();
        _Window.ShowWindow();
    }

    public static void OpenWindow() {
        Shared.Show();
    }

    public static void CloseWindow() {
        if (Shared._Window.IsShown) {
            Shared._Window.CloseWindow();
        }
    }

    public static void Toggle() {
        if (Shared._Window.IsShown) {
            Shared._Window.CloseWindow();
        } else {
            Shared.Show();
        }
    }

    private Action<GameObject>? _OnSelected;
    public static void SelectTemplate(Action<GameObject> onSelected) {
        Shared._OnSelected = go => {
            CloseWindow();
            Shared._Cache.Clear();
            Shared._OnSelected = null;
            onSelected(go);
        };

        OpenWindow();
        Shared._Window.Title = "Map Editor - select template";
    }

    public void OnDisable() {
        _Panel?.Dispose();
        _Panel = null;
        _OnSelected = null;
    }

    private void Populate() {
        _Window.Title = "Map Editor - Scene Viewer";
        _Panel?.Dispose();
        _Panel = UIPanel.Create(_Window.contentRectTransform!, BuilderAssets, Build);
    }


    private int                                                    _SelectedRootIndex;
    private Dictionary<int, (GameObject transform, string path)[]> _Cache        = new();
    private string                                                 _Filter       = "";
    private UIState<string?>                                       _SelectedItem = new(null);
    private float                                                  _Levels       = 1;

    private int _SelectedPathIndex = 0;

    private void Build(UIPanelBuilder builder) {
        if (!WorldTransformer.TryGetShared(out var worldTransformer) || worldTransformer == null) {
            builder.AddLabel("Error: Cannot find World");
            return;
        }

        var roots = worldTransformer.gameObject.transform.Cast<Transform>().Select(o => o.gameObject).OrderBy(o => o.name).ToArray();

        List<string> childrenNames = ["Select...", ..roots.Select(o => o.name)];
        builder.AddField("Children", builder.AddDropdown(childrenNames, _SelectedRootIndex, o => {
            _SelectedRootIndex = o;
            _SelectedComponentIndex = 0;
            _SelectedPathIndex = 0;
            builder.Rebuild();
        }));

        if (_SelectedRootIndex == 0) {
            builder.AddExpandingVerticalSpacer();
            return;
        }

        builder.AddField("Levels",
            builder.AddSlider(() => _Levels, () => _Levels.ToString("0"), o => _Levels = o, 0, 10, true, o => {
                _Levels = o;
                _SelectedItem.Value = null;
                _SelectedComponentIndex = 0;
                _Cache.Clear();
                builder.Rebuild();
            })!
        );

        if (!_Cache.TryGetValue(_SelectedRootIndex, out var listAll)) {
            var root = roots[_SelectedRootIndex - 1];
            listAll = GetAllDescendantsWithPath(root.transform, "", (int)_Levels).OrderBy(o => o.path).ToArray();
            _Cache.Add(_SelectedRootIndex, listAll);
        }

        builder.AddField("Filter", builder.AddInputField(_Filter, o => {
            _Filter = o;
            _SelectedItem.Value = null;
            _SelectedComponentIndex = 0;
            _SelectedPathIndex = 0;
            builder.Rebuild();
        }, "filter"));

        var data = listAll!
                   //.Where(o => o.transform.GetComponent<SceneryAssetInstance>() != null)
                   .Where(o => _Filter == "" || o.path.IndexOf(_Filter, StringComparison.InvariantCultureIgnoreCase) != -1)
                   .ToList();

        List<string> names = ["Select...", ..data.Select(o => o.path)];
        builder.AddField("Children", builder.AddDropdown(names, _SelectedPathIndex, o => {
            _SelectedPathIndex = o;
            builder.Rebuild();
        }));

        if (_SelectedPathIndex > 0 && data.Count > _SelectedPathIndex) {
            BuildDetail(builder, data[_SelectedPathIndex - 1].transform);
        }

        builder.AddExpandingVerticalSpacer();
    }

    private int _SelectedComponentIndex;

    private bool   _AutoRebuild;
    private Timer? _AutoRebuildTimer;

    private void BuildDetail(UIPanelBuilder builder, GameObject? go) {
        if (go == null) {
            builder.AddLabel("Select game object from drop down ...");
            return;
        }

        builder.RebuildOnEvent<RebuildSceneViewDialog>();

        builder.ButtonStrip(strip => { 
            strip.AddButton("Rebuild", () => builder.Rebuild());
            strip.AddButtonSelectable("Auto rebuild", _AutoRebuild, () => {
                _AutoRebuild = !_AutoRebuild;
                if (_AutoRebuild) {
                    _AutoRebuildTimer = go.AddComponent<Timer>();
                    _AutoRebuildTimer.Configure(new Action(builder.Rebuild), 1);
                } else {
                    if (_AutoRebuildTimer != null) {
                        Destroy(_AutoRebuildTimer);
                    }
                }
            });
            if (_OnSelected != null) {
                strip.AddButton("Clone this", () => _OnSelected(go)); 
            }
        });

        builder.AddField("activeSelf", () => $"{go.activeSelf}", UIPanelBuilder.Frequency.Periodic);
        builder.AddField("activeInHierarchy", () => $"{go.activeInHierarchy}", UIPanelBuilder.Frequency.Periodic);
        builder.AddField("Position", builder.AddInputField($"{go.transform.localPosition}", _ => { }));
        builder.AddField("Rotation", builder.AddInputField($"{go.transform.localEulerAngles}", _ => { }));
        builder.ButtonStrip(strip => {
            strip.AddButton("Show", Show(go));
            strip.AddButton("Move", Select(go, MoveableObjectMode.Move));
            strip.AddButton("Rotate", Select(go, MoveableObjectMode.Rotate));
        });
        builder.AddField("Path", builder.AddInputField($"{GetPath(go)}", _ => { }));

        var components = go.GetComponents<Component>()!.Where(o => o is not Transform or MoveableObject).OrderBy(o => o.GetType().Name).ToArray();
        if (components.Length == 0) {
            return;
        }

        if (_SelectedComponentIndex >= components.Length) {
            _SelectedComponentIndex = 0;
        }

        var componentNames = components.Select(o => o.GetType().Name).ToList();
        builder.AddField("Components", builder.AddDropdown(componentNames, _SelectedComponentIndex, o => {
            _SelectedComponentIndex = o;
            builder.Rebuild();
        }));

        BuildSectionForComponent(builder, components[_SelectedComponentIndex]);
    }

    private static IEnumerable<(GameObject transform, string path)> GetAllDescendantsWithPath(Transform parent, string parentPath, int level) {
        Log.Information("Children of '" + parentPath + "/" + parent.name + "': " + string.Join(", ", parent.Cast<Transform>().Select(o => o.name)));

        foreach (Transform child in parent) {
            var childPath = $"{parentPath}/{child.name}";
            yield return (child.gameObject, childPath);

            if (level == 0) {
                continue;
            }

            foreach (var descendant in GetAllDescendantsWithPath(child, childPath, level - 1)) {
                yield return descendant;
            }
        }
    }

    private static Action Show(GameObject gameObject) => () => CameraSelector.shared.ZoomToPoint(gameObject.transform.position.WorldToGame());

    private static Action Select(GameObject gameObject, MoveableObjectMode mode) => () => {
        //MoveableObject.Create(gameObject, mode, null, null, (_, _) => {
        //    MoveableObject.Destroy();
        //    Messenger.Default.Send(new RebuildSceneViewDialog());
        //});
    };

    private static string GetPath(GameObject gameObject) {
        var names = new Stack<string>();

        var transform = gameObject.transform;
        while (transform.parent != null!) {
            names.Push(transform.name);
            transform = transform.parent;
        }

        names.Push(transform.name);

        return string.Join("/", names);
    }

    private static void BuildSectionForComponent(UIPanelBuilder builder, Component component) {
        switch (component) {
            case SceneryAssetInstance sceneryAssetInstance:
                builder.AddField("Identifier", sceneryAssetInstance.identifier!);
                break;

            case Graph:
                builder.AddField("", builder.AddLabel("Graph contains all track nodes, segments, spans, etc."));
                break;

            default:
                var componentType = component.GetType();
                var fields        = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields) {
                    object? value;
                    try {
                        value = field.GetValue(component);
                    } catch (Exception e) {
                        value = "Exception: " + e.Message;
                    }

                    builder.AddField(field.Name, value?.ToString() ?? "<NULL>");
                }

                var readableProperties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                      .Where(o => o.CanRead && o.GetIndexParameters().Length == 0);

                foreach (var readableProperty in readableProperties) {
                    object? value;
                    try {
                        value = readableProperty.GetValue(component);
                    } catch (Exception e) {
                        value = "Exception: " + e.Message;
                    }

                    builder.AddField(readableProperty.Name, value?.ToString() ?? "<NULL>");
                }

                break;
        }
    }

    private record RebuildSceneViewDialog;
}
