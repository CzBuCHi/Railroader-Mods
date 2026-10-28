using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tweaks
{
    internal sealed class Settings
    {
        internal static Settings Current { get; set; } = new Settings();
        public bool SleepUntilNextInterchange { get; set; } = true;
        public bool ShowCarSpeed { get; set; } = true;
        public bool ShowCarOil { get; set; } = true;
        public bool ShowTrainLengthInMeters { get; set; } = true;
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
                // Reject typos and incorrect scalar types instead of silently changing intent.
                foreach (var property in json.Properties())
                {
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
                return settings;
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is JsonException)
            {
                warning("Cannot load Tweaks settings at " + path + ": " + error.Message + " Using defaults; existing file was not changed.");
                return new Settings();
            }
        }
    }
}
