using System;
using System.Linq;
using UI.Builder;
using UI.Common;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Teleporter;

internal sealed class TeleporterWindow : MonoBehaviour
{
    private TeleporterPlugin plugin = null!;
    private TeleporterRuntime runtime = null!;
    private Window window = null!;
    private UIBuilderAssets assets = null!;
    private UIPanel? panel;
    private UIPanelBuilder? rootBuilder;
    private readonly UIState<string> selectedGroup = new UIState<string>("");
    private string newName = "";
    private string newGroup = "";
    private string? confirmation;
    private TMP_InputField? groupInput;

    internal void Initialize(TeleporterPlugin owner, TeleporterRuntime host, Window nativeWindow, UIBuilderAssets builderAssets)
    {
        plugin = owner; runtime = host; window = nativeWindow; assets = builderAssets;
        window.Title = "Teleporter";
        // Keep every panel graphic within the native content rectangle.
        if (window.contentRectTransform.GetComponent<RectMask2D>() == null)
            window.contentRectTransform.gameObject.AddComponent<RectMask2D>();
        newGroup = plugin.Settings.Groups.FirstOrDefault()?.Name ?? "";
        selectedGroup.Value = newGroup;
    }

    internal void Toggle() { if (window.IsShown) window.CloseWindow(); else Show(); }
    internal void Show() { Populate(); window.ShowWindow(); }
    private void Populate()
    {
        panel?.Dispose();
        panel = UIPanel.Create(window.contentRectTransform, assets, Build);
    }
    private void Changed(UIPanelBuilder builder) { plugin.Save(); rootBuilder?.Rebuild(); }

    private void Build(UIPanelBuilder builder)
    {
        rootBuilder = builder;
        builder.ButtonStrip(strip => strip.AddButton("Save current", () => SaveCurrent(builder))
            .Tooltip("Save current position", "Save the active first-person character or tracking camera position."));
        var field = builder.AddInputField(newGroup, value => newGroup = value, "Group for new location", 80);
        groupInput = field.GetComponent<TMP_InputField>();
        builder.AddField("Group name", field);
        builder.AddField("New location name", builder.AddInputField(newName, value => newName = value,
            "Unique name for new location", 80));
        var groups = plugin.Settings.Groups.Select(group =>
            new UIPanelBuilder.ListItem<LocationGroup>(group.Name, group, "", group.Name)).ToList();
        builder.AddListDetail(groups, selectedGroup, (detail, group) => BuildGroup(detail, group), listWidth: 140f);
    }

    private void BuildGroup(UIPanelBuilder builder, LocationGroup? group)
    {
        // Native ListDetailController builds an empty detail before applying selection.
        if (group == null)
        {
            builder.AddLabelEmptyState(plugin.Settings.Groups.Count == 0
                ? "Saved locations will be shown here..." : "Select a group to view locations...");
            return;
        }
        // Selecting a group fills the original save form without resetting typed names.
        if (newGroup != group.Name)
        {
            newGroup = group.Name;
            groupInput?.SetTextWithoutNotify(newGroup);
        }
        builder.FieldLabelWidth = 100f;
        var groups = plugin.Settings.Groups;
        var index = groups.IndexOf(group);
        if (index < 0) return;
        builder.AddField("Group", builder.ButtonStrip(strip => {
            strip.AddButtonCompact("▲", () => MoveGroup(group, -1, builder)).Tooltip("Move group up", "Move this group up in the list.").Disable(index == 0);
            strip.AddButtonCompact("▼", () => MoveGroup(group, 1, builder)).Tooltip("Move group down", "Move this group down in the list.").Disable(index == groups.Count - 1);
        }));
        if (group.Locations.Count == 0)
        {
            builder.AddLabelEmptyState("Saved locations will be shown here...");
            return;
        }
        builder.VScrollView(scroll => {
            scroll.FieldLabelWidth = 110f;
            foreach (var pair in group.Locations.OrderBy(item => item.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                var key = pair.Key;
                var location = pair.Value;
                var token = group.Name + "\n" + key;
                scroll.AddField(key, scroll.ButtonStrip(strip => {
                    strip.AddButtonCompact(TextSprites.Destination, () => runtime.Teleport(location))
                        .Tooltip("Teleport", location.FirstPerson ? "Return to the saved first-person position." : "Return to the saved tracking camera view.");
                    strip.AddButtonCompact(confirmation == "replace:" + token ? TextSprites.Warning : TextSprites.Copy, () => {
                        if (confirmation != "replace:" + token)
                        { confirmation = "replace:" + token; rootBuilder?.Rebuild(); return; }
                        try { group.Locations[key] = CameraBookmarks.Capture(); confirmation = null; Changed(builder); }
                        catch (Exception error) { Toast.Present(error.Message); }
                    }).Tooltip(confirmation == "replace:" + token ? "Confirm replace" : "Replace", "Save the current position under this name. Click twice to confirm.");
                    strip.AddButtonCompact(confirmation == "remove:" + token ? "Confirm" : "×", () => {
                        if (confirmation != "remove:" + token)
                        { confirmation = "remove:" + token; rootBuilder?.Rebuild(); return; }
                        group.Locations.Remove(key); confirmation = null; Changed(builder);
                    }).Tooltip(confirmation == "remove:" + token ? "Confirm remove" : "Remove", "Remove this saved location. Click twice to confirm.");
                }));
            }
        });
    }

    private void MoveGroup(LocationGroup group, int delta, UIPanelBuilder builder)
    {
        var groups = plugin.Settings.Groups;
        var index = groups.IndexOf(group);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= groups.Count) return;
        groups[index] = groups[next]; groups[next] = group;
        Changed(builder);
    }

    private void SaveCurrent(UIPanelBuilder builder)
    {
        var name = newName.Trim();
        var groupName = newGroup.Trim();
        if (name.Length == 0 || groupName.Length == 0)
        { Toast.Present("Enter a group name and a location name."); return; }
        var groups = plugin.Settings.Groups;
        var group = groups.FirstOrDefault(item => item.Name == groupName);
        if (group != null && group.Locations.ContainsKey(name))
        { Toast.Present("This location already exists. Use Replace to update it."); return; }
        try
        {
            var location = CameraBookmarks.Capture();
            if (group == null)
            {
                group = new LocationGroup { Name = groupName };
                groups.Add(group);
            }
            group.Locations[name] = location;
            selectedGroup.Value = groupName;
            newGroup = groupName; newName = ""; confirmation = null;
            Changed(builder);
        }
        catch (Exception error) { Toast.Present(error.Message); }
    }

    private void OnDisable() { panel?.Dispose(); panel = null; }
    private void OnDestroy() { panel?.Dispose(); panel = null; }
}