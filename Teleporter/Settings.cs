using System.Collections.Generic;

namespace Teleporter;

public sealed class Settings
{
    public List<LocationGroup> Groups                   { get; } = new();
    public bool                AutoOpenTeleporterWindow { get; set; }
}

public sealed record LocationGroup(string Name, Dictionary<string, TeleportLocation> Locations);

public record Vector(float X, float Y, float Z);

public record TeleportLocation(Vector Position, Vector Rotation, float Distance);
