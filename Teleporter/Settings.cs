using System.Collections.Generic;

namespace Teleporter;

public class Settings
{
    public Dictionary<string, TeleportLocation> Locations  { get; } = new();
    public bool                                 CloseAfter { get; set; }
}

public record Vector(float X, float Y, float Z);

public record TeleportLocation(Vector Position, Vector Rotation, float Distance);
