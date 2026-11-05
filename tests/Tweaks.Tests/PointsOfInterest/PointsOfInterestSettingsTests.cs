using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using System;
using Newtonsoft.Json.Linq;
using Tweaks.PointsOfInterest.Bookmarks;
using Tweaks.PointsOfInterest.Configuration;
using Tweaks.Tests;
using RootSettings = Tweaks.Configuration.Settings;

namespace Tweaks.Tests.PointsOfInterest
{
    internal static class PointsOfInterestSettingsTests
    {
        internal static void Run(string root)
        {
            var path = Path.Combine(root, "points", "tweaks.json");
            RootSettings.Current = RootSettings.Load(path, message => throw new Exception(message));
            var defaults = JObject.Parse(File.ReadAllText(path));
            Program.Check((string?)defaults["PointsOfInterest"]!["OpenWindowShortcut"] == "Alt+T", "Default shortcut is stored with the mod settings");
            var shortcutSettings = RootSettings.Current.PointsOfInterest;
            var shortcut = shortcutSettings.ParseOpenWindowShortcut();
            Program.Check(shortcut.key == Key.T && shortcut.modifiers.SequenceEqual(new[] { "alt" }), "Default shortcut resolves to Alt plus T");
            shortcutSettings.OpenWindowShortcut = "Ctrl + Shift + F2";
            shortcut = shortcutSettings.ParseOpenWindowShortcut();
            Program.Check(shortcut.key == Key.F2 && shortcut.modifiers.SequenceEqual(new[] { "ctrl", "shift" }), "Custom shortcut supports multiple modifiers and function keys");
            shortcutSettings.OpenWindowShortcut = "9";
            Program.Check(shortcutSettings.ParseOpenWindowShortcut().key == Key.Digit9, "Digit keys work without modifiers");
            shortcutSettings.OpenWindowShortcut = "Alt+T";
            Program.Check(defaults["PointsOfInterest"] is JObject && !(bool)defaults["PointsOfInterest"]!["AutoOpenWindow"]!
                && !defaults["PointsOfInterest"]!["Groups"]!.Any(), "New Tweaks files contain nested POI defaults");
            defaults["ShowCarSpeed"] = false;
            File.WriteAllText(path, defaults.ToString());
            var store = new BookmarkStore(path);
            store.Add("Yard", "View", new TeleportLocation { Position = new Vector { X = 12 }, Distance = 30 });
            store.SetAutoOpen(true);
            var saved = JObject.Parse(File.ReadAllText(path));
            Program.Check(!(bool)saved["ShowCarSpeed"]! && (bool)saved["PointsOfInterest"]!["AutoOpenWindow"]!
                && saved["Groups"] == null && saved["AutoOpenWindow"] == null, "POI edits preserve sibling settings and stay nested");
            Program.Check(new BookmarkStore(path).Get("Yard", "View").Position.X == 12
                && RootSettings.Load(path, message => throw new Exception(message)).PointsOfInterest.AutoOpenWindow,
                "Both root settings and bookmark storage load the same nested data");
            Program.Check(!File.ReadAllText(path).Contains("AutoOpenTeleporterWindow")
                && !File.ReadAllText(path).Contains("AutoOpenPointsOfInterestWindow"), "Only the new AutoOpenWindow name is written");
            saved["UseMeters"] = false;
            File.WriteAllText(path, saved.ToString());
            store.Rename("Yard", "View", "Station", "Renamed");
            Program.Check(!(bool)JObject.Parse(File.ReadAllText(path))["UseMeters"]!
                && new BookmarkStore(path).Get("Station", "Renamed").Position.X == 12, "Edits preserve newer sibling changes made on disk");
            Program.Check(JObject.Parse(File.ReadAllText(path + ".bak")).ToString() == saved.ToString(), "Atomic save backs up the entire previous Tweaks file");
            var bytes = File.ReadAllBytes(path);
            Program.Check(bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 })
                && !File.ReadAllText(path).Replace("\r\n", "").Contains('\n'), "Merged settings use UTF-8 BOM and CRLF");
            using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var failed = false;
                try { store.SetAutoOpen(false); } catch (IOException) { failed = true; }
                Program.Check(failed && store.Current.AutoOpenWindow, "Failed writes leave the current POI settings unchanged");
            }
            var sparse = JObject.Parse("{\"ShowCarSpeed\":false,\"MapTweaks\":{},\"PointsOfInterest\":{\"AutoOpenWindow\":true,\"Groups\":[{\"Name\":\"Yard\",\"Locations\":{\"View\":{\"Position\":{\"X\":12345.123456789},\"Distance\":30}}}]}}");
            File.WriteAllText(path, sparse.ToString());
            RootSettings.Load(path, message => throw new Exception(message));
            sparse["PointsOfInterest"]!["OpenWindowShortcut"] = "Alt+T";
            Program.Check(JToken.DeepEquals(sparse, JObject.Parse(File.ReadAllText(path))),
                "Adding the default shortcut preserves exact saved bookmark numbers and omitted fields");
            foreach (var invalid in new[] {
                "{", "{\"PointsOfInterest\":null}", "{\"PointsOfInterest\":{\"AutoOpenWindow\":\"true\"}}",
                "{\"PointsOfInterest\":{\"AutoOpenTeleporterWindow\":true}}", "{\"Groups\":[]}",
                "{\"PointsOfInterest\":{\"Groups\":null}}", "{\"PointsOfInterest\":{\"Groups\":[null]}}",
                "{\"ShowCarSpeed\":\"false\"}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":null}}", "{\"PointsOfInterest\":{\"OpenWindowShortcut\":123}}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"\"}}", "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"Alt+Alt+T\"}}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"Bad+T\"}}", "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"Alt+NoSuchKey\"}}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"Alt+\"}}", "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"None\"}}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"A,B\"}}",
                "{\"PointsOfInterest\":{\"OpenWindowShortcut\":\"Ctrl+Shift+Alt+Meta+T\"}}" })
            {
                File.WriteAllText(path, invalid);
                var warnings = 0;
                var fallback = RootSettings.Load(path, _ => warnings++);
                Program.Check(warnings == 1 && !fallback.PointsOfInterest.AutoOpenWindow && File.ReadAllText(path) == invalid,
                    "Invalid nested/root data is reported without rewriting it");
                var failed = false;
                try { store.SetAutoOpen(false); } catch (Exception) { failed = true; }
                Program.Check(failed && File.ReadAllText(path) == invalid && store.Current.AutoOpenWindow,
                    "POI edits refuse to overwrite an invalid Tweaks file");
            }
        }
    }
}
