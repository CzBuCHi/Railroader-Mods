using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityModManagerNet;

namespace Teleporter
{
    public class Settings : UnityModManager.ModSettings
    {
        [DataMember]
        public List<LocationGroup> Groups { get; } = new();

        [DataMember]
        [Draw("Auto Open Teleporter Window", DrawType.Toggle)] // TODO: this is not working ...
        public bool AutoOpenTeleporterWindow;

        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }

        public void OnChange() {
        }
    }

    public sealed record LocationGroup(string Name, Dictionary<string, TeleportLocation> Locations);

    public sealed record Vector(float X, float Y, float Z);

    public sealed record TeleportLocation(Vector Position, Vector Rotation, float Distance);
}
