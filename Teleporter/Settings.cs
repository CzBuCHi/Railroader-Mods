using System.Collections.Generic;

namespace Teleporter;

public sealed class Settings
{
    public List<LocationGroup> Groups { get; set; } = new List<LocationGroup>();
    public bool AutoOpenTeleporterWindow { get; set; }
}

public sealed class LocationGroup
{
    public string Name { get; set; } = "";
    public Dictionary<string, TeleportLocation> Locations { get; set; } = new Dictionary<string, TeleportLocation>();
}

public sealed class Vector
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public sealed class TeleportLocation
{
    public Vector Position { get; set; } = new Vector();
    public Vector Rotation { get; set; } = new Vector();
    public float Distance { get; set; }
    // Missing in legacy files: false retains the original tracking-camera behavior.
    public bool FirstPerson { get; set; }
}