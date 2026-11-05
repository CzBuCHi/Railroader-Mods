using System.IO;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Tweaks.Configuration
{
    internal sealed class Settings
    {
        internal static Settings Current { get; set; } = new Settings();
        internal static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Railroader", "ModSettings", "czbuchi", "tweaks.json");
        public global::Tweaks.MapTweaks.Configuration.Settings MapTweaks { get; set; } = new global::Tweaks.MapTweaks.Configuration.Settings();
        public global::Tweaks.PointsOfInterest.Configuration.Settings PointsOfInterest { get; set; } = new global::Tweaks.PointsOfInterest.Configuration.Settings();
        public bool SleepUntilNextInterchange { get; set; } = true;
        public bool ShowCarSpeed { get; set; } = true;
        public bool ShowCarOil { get; set; } = true;
        public bool UseMeters { get; set; } = true;
        public bool ShowAirButton { get; set; } = true;
        public bool ShowHandbrakeButton { get; set; } = true;
        public bool ShowOilButton { get; set; } = true;
        public bool ShowControlModeButtons { get; set; } = true;
        public bool RosterShiftFollow { get; set; } = true;
        public bool FasterManualPushing { get; set; } = true;
        public bool CopyCrew { get; set; } = true;
        public bool CopyRepairDestination { get; set; } = true;
        public float OilWarningPercent { get; set; } = 50f;
        public float ManualPushSpeedMph { get; set; } = 6f;

        internal static Settings Load(string path, Action<string> warning)
        {
            try
            {
                if (!File.Exists(path))
                {
                    var defaults = new Settings();
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, JsonConvert.SerializeObject(defaults, Formatting.Indented)
                        .Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n", new UTF8Encoding(true));
                    return defaults;
                }
                var json = JObject.Parse(File.ReadAllText(path));
                var settings = Parse((JObject)json.DeepClone());
                if (json[nameof(MapTweaks)] == null)
                    WriteSection(path, nameof(MapTweaks), JObject.FromObject(settings.MapTweaks));
                var points = json[nameof(PointsOfInterest)] as JObject;
                if (points == null || points["OpenWindowShortcut"] == null)
                {
                    var node = points == null ? JObject.FromObject(settings.PointsOfInterest) : (JObject)points.DeepClone();
                    node["OpenWindowShortcut"] = settings.PointsOfInterest.OpenWindowShortcut;
                    WriteSection(path, nameof(PointsOfInterest), node);
                }
                return settings;
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is JsonException || error is ArgumentException)
            {
                warning("Cannot load Tweaks settings at " + path + ": " + error.Message + " Using defaults; existing file was not changed.");
                return new Settings();
            }
        }

        internal static Settings Parse(JObject json)
        {
            // Read older profiles without resetting their unrelated settings.
            bool? legacyMeters = null;
            foreach (var key in new[] { "ShowDistancesInMeters", "ShowTrainLengthInMeters" })
            {
                if (!json.TryGetValue(key, out var value)) continue;
                if (value.Type != JTokenType.Boolean) throw new InvalidDataException("Invalid setting: " + key);
                legacyMeters = (legacyMeters ?? false) || value.Value<bool>();
                json.Remove(key);
            }
            if (json["UseMeters"] == null && legacyMeters.HasValue) json["UseMeters"] = legacyMeters.Value;
            // Reject typos and incorrect scalar types instead of silently changing intent.
            foreach (var property in json.Properties())
            {
                if (property.Name == nameof(MapTweaks))
                {
                    if (!(property.Value is JObject map)) throw new InvalidDataException("MapTweaks must be an object.");
                    global::Tweaks.MapTweaks.Configuration.Settings.ValidateJson(map);
                    continue;
                }
                if (property.Name == nameof(PointsOfInterest))
                {
                    if (!(property.Value is JObject points)) throw new InvalidDataException("PointsOfInterest must be an object.");
                    global::Tweaks.PointsOfInterest.Bookmarks.BookmarkStore.ValidateJson(points);
                    continue;
                }
                var member = typeof(Settings).GetProperty(property.Name);
                if (member == null || (member.PropertyType == typeof(bool)
                    ? property.Value.Type != JTokenType.Boolean
                    : property.Value.Type != JTokenType.Integer && property.Value.Type != JTokenType.Float))
                    throw new InvalidDataException("Invalid setting: " + property.Name);
            }
            var settings = json.ToObject<Settings>()!;
            if (float.IsNaN(settings.OilWarningPercent) || settings.OilWarningPercent < 0f || settings.OilWarningPercent > 100f)
                throw new InvalidDataException("OilWarningPercent must be between 0 and 100.");
            if (float.IsNaN(settings.ManualPushSpeedMph) || settings.ManualPushSpeedMph < 1f || settings.ManualPushSpeedMph > 30f)
                throw new InvalidDataException("ManualPushSpeedMph must be between 1 and 30.");
            global::Tweaks.PointsOfInterest.Bookmarks.BookmarkStore.Validate(settings.PointsOfInterest);
            settings.MapTweaks.Validate();
            return settings;
        }

        internal static void WriteSection(string path, string name, JObject value)
        {
            var json = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : JObject.FromObject(Current);
            Parse((JObject)json.DeepClone());
            json[name] = value;
            Parse((JObject)json.DeepClone());
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            try
            {
                var text = json.ToString(Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
                File.WriteAllText(temporary, text, new UTF8Encoding(true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
