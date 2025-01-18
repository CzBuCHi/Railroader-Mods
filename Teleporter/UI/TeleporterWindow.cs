using System;
using System.Linq;
using CzBuCHi.Shared.UI;
using Helpers;
using JetBrains.Annotations;
using Teleporter.Extensions;
using Teleporter.Harmony;
using UI.Builder;
using UI.Common;

namespace Teleporter.UI;

[PublicAPI]
public sealed class TeleporterWindow : ProgrammaticWindowBase
{
    public override Window.Sizing Sizing => Window.Sizing.Resizable(new(400, 250));

    private string _NewLocationName = "";

    private static TeleporterWindow Shared => WindowManager.Shared!.GetWindow<TeleporterWindow>()!;

    public static void Toggle() {
        if (Shared.Window.IsShown) {
            Shared.Window.CloseWindow();
        } else {
            Shared.ShowWindow();
        }
    }

    protected override void Build(UIPanelBuilder builder) {
        builder.ButtonStrip(strip => {
            strip.AddButton("Save current", Add())!
                 .Tooltip("Save current position", "Save current location under new name");
            strip.AddFieldToggle("Close after teleport", () => TeleporterPlugin.Settings.CloseAfter, UpdateSettings);
        });
        builder.AddField("New location name", builder.AddInputField(_NewLocationName, UpdateNewLocationName, "Unique name for new location")!);
        builder.VScrollView(scroll => {
            foreach (var pair in TeleporterPlugin.Settings.Locations.OrderBy(o => o.Key)) {
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

        void UpdateSettings(bool value) {
            TeleporterPlugin.Settings.CloseAfter = value;
            TeleporterPlugin.SaveSettings();
        }

        Action Add() => () => {
            if (TeleporterPlugin.Settings.Locations.ContainsKey(_NewLocationName)) {
                Toast.Present("Location with name " + _NewLocationName + " already exists.");
                return;
            }

            Save(_NewLocationName);
        };

        Action Replace(string key) => () => Save(key);

        void UpdateNewLocationName(string value) {
            _NewLocationName = value;
            builder.Rebuild();
        }

        void Save(string key) {
            var camera = CameraSelector.shared!.strategyCamera!;
            TeleporterPlugin.Settings.Locations[key] = new TeleportLocation(
                WorldTransformer.WorldToGame(camera.GroundPosition).ToVector(),
                camera.transform!.rotation.ToVector(),
                camera.GetDistance()
            );

            TeleporterPlugin.SaveSettings();
            builder.Rebuild();
        }

        Action TeleportTo(TeleportLocation location) {
            return () => {
                var point    = location.Position.ToVector3();
                var rotation = location.Rotation.ToQuaternion();
                CameraSelector.shared!.SelectCamera(CameraSelector.CameraIdentifier.Strategy);

                var camera = CameraSelector.shared!.strategyCamera!;
                camera.SetAngleX(rotation.eulerAngles.x);
                camera.SetDistance(location.Distance);
                camera.JumpTo(point, rotation);

                if (TeleporterPlugin.Settings.CloseAfter) {
                    CloseWindow();
                }
            };
        }

        Action Remove(string key) {
            return () => {
                TeleporterPlugin.Settings.Locations.Remove(key);
                TeleporterPlugin.SaveSettings();
                builder.Rebuild();
            };
        }
    }
}