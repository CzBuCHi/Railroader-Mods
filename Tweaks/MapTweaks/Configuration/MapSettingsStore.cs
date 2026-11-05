using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using RootSettings = Tweaks.Configuration.Settings;

namespace Tweaks.MapTweaks.Configuration
{
    internal sealed class MapSettingsStore
    {
        private readonly string path;
        internal MapSettingsStore(string path) { this.path = path; }

        internal Settings Load() => File.Exists(path)
            ? RootSettings.Parse(JObject.Parse(File.ReadAllText(path))).MapTweaks
            : new Settings();

        internal void SaveSwitches(IEnumerable<string> ids) => Change(settings => settings.ThrownSwitches = ids
            .Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray());

        internal void Change(Action<Settings> change)
        {
            // Read the latest node so independent map actions and POI edits do not overwrite one another.
            var next = Load();
            change(next);
            next.Validate();
            RootSettings.WriteSection(path, "MapTweaks", JObject.FromObject(next));
            RootSettings.Current.MapTweaks = next;
        }
    }
}
