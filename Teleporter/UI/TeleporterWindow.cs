﻿using System;
using System.Collections.Generic;
using System.Linq;
using CzBuCHi.Shared.UI;
using Helpers;
using JetBrains.Annotations;
using Teleporter.Events;
using Teleporter.Extensions;
using Teleporter.Harmony;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace Teleporter.UI;

[PublicAPI]
public sealed class TeleporterWindow : ProgrammaticWindowBase
{
    public override Window.Sizing Sizing => Window.Sizing.Resizable(new(400, 250));

    private string          _NewLocationGroup = "Unnamed";
    private string          _NewLocationName  = "";
    private UIState<string> _SelectedGroup    = new("");

    private HashSet<string> _Groups;

    private static TeleporterWindow Shared => WindowManager.Shared!.GetWindow<TeleporterWindow>()!;

    public override void Awake() {
        base.Awake();
        Window.Title = "Teleporter";

        _Groups = new HashSet<string>(TeleporterPlugin.Settings.Groups.Select(o => o.Name));
        _SelectedGroup.Value = _Groups.FirstOrDefault() ?? "";
    }

    protected override void WindowOnOnShownDidChange(bool isShown) {
        base.WindowOnOnShownDidChange(isShown);
        if (isShown) {
            var rectTransform = Window.GetComponent<RectTransform>()!;
            rectTransform.position = new Vector2(Screen.width, 0).Round();
        }
    }

    public static void Toggle() {
        if (Shared.Window.IsShown) {
            Shared.Window.CloseWindow();
        } else {
            Shared.ShowWindow();
        }
    }

    protected override void Build(UIPanelBuilder builder) {
        builder.RebuildOnEvent<SettingsChangedEvent>();

        builder.ButtonStrip(strip => {
            strip.AddButton("Save current", Add)
                 .Tooltip("Save current position", "Save current location under new name");            
        });
        builder.AddField("New location group", builder.AddInputField(_NewLocationGroup, UpdateNewLocationGroup, "Group name for new location"));
        builder.AddField("New location name", builder.AddInputField(_NewLocationName, UpdateNewLocationName, "Unique name for new location"));

        var groups = _Groups
                     .Select(o => new UIPanelBuilder.ListItem<string>(o, o, "", o))
                     .ToList();
     
        builder.AddListDetail(groups, _SelectedGroup, BuildGroup);

        return;
       
        void UpdateNewLocationGroup(string value) {
            _NewLocationGroup = value;
            builder.Rebuild();
        }

        void UpdateNewLocationName(string value) {
            _NewLocationName = value;
            builder.Rebuild();
        }
    }

    private void BuildGroup(UIPanelBuilder builder, string groupName) {
        builder.RebuildOnEvent<SettingsChangedEvent>();

        if (string.IsNullOrEmpty(groupName)) {
            var message = TeleporterPlugin.Settings.Groups.Count > 0
                ? "Select group to view locations ..."
                : "Saved locations will be shown here ...";
            builder.AddLabel(message);
            return;
        }

        var group = TeleporterPlugin.Settings.Groups.FirstOrDefault(o => o.Name == groupName);
        if (group == null) {
            builder.AddLabel("Something went wrong ... (cannot find group named '" + groupName + "'");
            return;
        }

        builder.VScrollView(scroll => {
            foreach (var pair in group.Locations.OrderBy(o => o.Key)) {
                scroll.AddField(pair.Key!,
                    scroll.ButtonStrip(strip => {
                        strip.AddButton(TextSprites.Destination, TeleportTo(pair.Value!))!.Tooltip("Teleport", "Switch to strategy camera at given location");
                        strip.Spacer();
                        strip.AddButton("Replace", Replace(pair.Key!))!.Tooltip("Replace", "Save current location under this name");
                        strip.AddButton("Remove", Remove(pair.Key!))!.Tooltip("Remove location", "Remove location from saved locations");
                    })!
                );
            }
        });

        return;

        Action TeleportTo(TeleportLocation location) {
            return () => {
                var point    = location.Position.ToVector3();
                var rotation = location.Rotation.ToQuaternion();
                CameraSelector.shared!.SelectCamera(CameraSelector.CameraIdentifier.Strategy);

                var camera = CameraSelector.shared!.strategyCamera!;
                camera.SetAngleX(rotation.eulerAngles.x);
                camera.SetDistance(location.Distance);
                camera.JumpTo(point, rotation);
            };
        }

        Action Replace(string key) => () => Save(group, key);

        Action Remove(string key) {
            return () => {
                group.Locations.Remove(key);
                if (group.Locations.Count == 0) {
                    TeleporterPlugin.Settings.Groups.Remove(group);
                }

                TeleporterPlugin.SaveSettings();
            };
        }
    }
    
    private void Add() {
        var group = TeleporterPlugin.Settings.Groups.FirstOrDefault(o => o.Name == _NewLocationGroup);
        if (group == null) {
            group = new LocationGroup(_NewLocationGroup, new Dictionary<string, TeleportLocation>());
            TeleporterPlugin.Settings.Groups.Add(group);
        } else {
            if (group.Locations.ContainsKey(_NewLocationName)) {
                Toast.Present("Location with name " + _NewLocationName + " already exists in group + " + _NewLocationGroup + ".");
                return;
            }
        }

        Save(group, _NewLocationName);
    }

    private void Save(string groupName, string locationName) {
        var group = TeleporterPlugin.Settings.Groups.FirstOrDefault(o => o.Name == _NewLocationGroup);
        if (group == null) {
            group = new LocationGroup(_NewLocationGroup, new Dictionary<string, TeleportLocation>());
            TeleporterPlugin.Settings.Groups.Add(group);
        } 

        Save(group, locationName);
    }

    private void Save(LocationGroup group, string locationName) {
        var camera = CameraSelector.shared.strategyCamera;

        group.Locations[locationName] = new TeleportLocation(
            WorldTransformer.WorldToGame(camera.GroundPosition).ToVector(),
            camera.transform.rotation.ToVector(),
            camera.GetDistance()
        );

        TeleporterPlugin.SaveSettings();
    }
}