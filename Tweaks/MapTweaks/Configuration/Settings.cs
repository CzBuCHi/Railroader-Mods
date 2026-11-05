using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Tweaks.MapTweaks.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VehicleDisplayMode
    {
        LocomotivesOnly,
        LocomotivesAndCabooses,
        AllVehicles
    }

    public sealed class Settings
    {
        public bool Enabled { get; set; } = true;
        public VehicleDisplayMode VehicleDisplayMode { get; set; } = VehicleDisplayMode.LocomotivesOnly;
        public bool FollowSelectedCar { get; set; }
        public string[] ThrownSwitches { get; set; } = Array.Empty<string>();

        internal static void ValidateJson(JObject json)
        {
            foreach (var property in json.Properties())
            {
                if ((property.Name == nameof(Enabled) || property.Name == nameof(FollowSelectedCar))
                    && property.Value.Type == JTokenType.Boolean) continue;
                if (property.Name == nameof(VehicleDisplayMode))
                {
                    if (property.Value.Type != JTokenType.String || !Enum.GetNames(typeof(VehicleDisplayMode))
                        .Contains((string?)property.Value, StringComparer.Ordinal))
                        throw new InvalidDataException("MapTweaks.VehicleDisplayMode must be LocomotivesOnly, LocomotivesAndCabooses, or AllVehicles.");
                    continue;
                }
                if (property.Name == nameof(ThrownSwitches) && property.Value is JArray ids
                    && ids.All(id => id.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string?)id))) continue;
                throw new InvalidDataException("Invalid MapTweaks setting: " + property.Name);
            }
        }

        internal void Validate()
        {
            if (!Enum.IsDefined(typeof(VehicleDisplayMode), VehicleDisplayMode))
                throw new InvalidDataException("Invalid MapTweaks.VehicleDisplayMode.");
            if (ThrownSwitches == null || ThrownSwitches.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("MapTweaks.ThrownSwitches must contain nonempty switch IDs.");
        }
    }
}
