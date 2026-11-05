using System.IO;
using System.Linq;
using System.Text;
using System;
using Newtonsoft.Json;
using Tweaks.Hud;
using Tweaks.Tests.PointsOfInterest;
using Tweaks.Tests;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Tests.Configuration
{
    internal static class SettingsTests
    {
        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "Tweaks.SettingsTests." + Guid.NewGuid());
            var path = Path.Combine(root, "profile", "tweaks.json");
            var warnings = 0;
            Settings Load() => Settings.Load(path, _ => warnings++);
            try
            {
                PointsOfInterestSettingsTests.Run(root);
                var defaults = Load();
                Program.Check(File.Exists(path) && warnings == 0, "First load creates settings and parent directory");
                var bytes = File.ReadAllBytes(path);
                var generated = File.ReadAllText(path);
                Program.Check(generated.Contains("\"UseMeters\": true") && !generated.Contains("ShowDistancesInMeters") &&
                    !generated.Contains("ShowTrainLengthInMeters"), "Generated defaults contain only the unified UseMeters option");
                Program.Check(bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 }) &&
                    !File.ReadAllText(path).Replace("\r\n", "").Contains('\n'), "Settings use UTF-8 BOM and CRLF");
                Program.Check(defaults.OilWarningPercent == 50 && defaults.ManualPushSpeedMph == 6 &&
                    typeof(Settings).GetProperties().Where(p => p.PropertyType == typeof(bool)).All(p => (bool)p.GetValue(defaults)!),
                    "Defaults retain all existing features");
                File.WriteAllText(path, "{\"ShowCarSpeed\":false,\"OilWarningPercent\":25,\"ManualPushSpeedMph\":9}");
                var custom = Load();
                Program.Check(!custom.ShowCarSpeed && custom.ShowCarOil && custom.OilWarningPercent == 25 && custom.ManualPushSpeedMph == 9,
                    "Partial JSON overrides values and retains defaults for missing keys");
                File.WriteAllText(path, "{\"UseMeters\":false}");
                Program.Check(!Load().UseMeters, "Unified metric option can be disabled");
                File.WriteAllText(path, "{\"ShowDistancesInMeters\":false,\"ShowTrainLengthInMeters\":true,\"ShowCarSpeed\":false}");
                var legacy = Load();
                Program.Check(legacy.UseMeters && !legacy.ShowCarSpeed, "Old metric options preserve other settings and enable meters if either was enabled");
                File.WriteAllText(path, "{\"UseMeters\":false,\"ShowDistancesInMeters\":true}");
                Program.Check(!Load().UseMeters, "Explicit unified metric option overrides old keys");
                foreach (var property in typeof(Settings).GetProperties().Where(p => p.PropertyType == typeof(bool))) property.SetValue(custom, false);
                File.WriteAllText(path, JsonConvert.SerializeObject(custom));
                var disabled = Load();
                Program.Check(typeof(Settings).GetProperties().Where(p => p.PropertyType == typeof(bool)).All(p => !(bool)p.GetValue(disabled)!),
                    "Every feature can be disabled in JSON");
                Settings.Current = custom;
                Program.Check(HudRules.OilWarningThreshold == 0.25f, "Oil warning uses configured percentage");
                foreach (var invalid in new[] { "null", "{", "{\"ShowCarSpeed\":null}", "{\"ShowCarSpeed\":\"false\"}",
                    "{\"ShowCarSpeeed\":false}", "{\"OilWarningPercent\":101}", "{\"OilWarningPercent\":-1}",
                    "{\"OilWarningPercent\":NaN}", "{\"ManualPushSpeedMph\":0}", "{\"ManualPushSpeedMph\":31}", "{\"ManualPushSpeedMph\":1e100}" })
                {
                    File.WriteAllText(path, invalid);
                    var before = warnings;
                    var fallback = Load();
                    Program.Check(warnings == before + 1 && fallback.ShowCarSpeed && fallback.ManualPushSpeedMph == 6 &&
                        File.ReadAllText(path) == invalid, "Invalid config is reported, preserved and defaults restored: " + invalid);
                }
            }
            finally
            {
                Settings.Current = new Settings();
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
