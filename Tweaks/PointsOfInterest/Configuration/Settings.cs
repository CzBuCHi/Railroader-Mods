using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;

namespace Tweaks.PointsOfInterest.Configuration
{

    public sealed class Settings
    {
        public List<LocationGroup> Groups { get; set; } = new List<LocationGroup>();
        public bool AutoOpenWindow { get; set; }
        public string OpenWindowShortcut { get; set; } = "Alt+T";

        internal (Key key, string[] modifiers) ParseOpenWindowShortcut()
        {
            if (string.IsNullOrWhiteSpace(OpenWindowShortcut))
                throw new InvalidDataException("PointsOfInterest.OpenWindowShortcut needs a key, for example Alt+T.");
            var parts = OpenWindowShortcut.Split('+').Select(part => part.Trim()).ToArray();
            var modifiers = new List<string>();
            foreach (var part in parts.Take(parts.Length - 1))
            {
                var modifier = part.ToLowerInvariant() switch {
                    "alt" => "alt", "ctrl" => "ctrl", "shift" => "shift", "meta" => "meta", _ => null
                };
                if (modifier == null || modifiers.Contains(modifier) || modifiers.Count == 3)
                    throw new InvalidDataException("Shortcut accepts up to three different modifiers: Alt, Ctrl, Shift, Meta.");
                modifiers.Add(modifier);
            }
            var name = parts[parts.Length - 1];
            if (name.Length == 1 && name[0] >= '0' && name[0] <= '9') name = "Digit" + name;
            // Enum.TryParse also accepts numbers, so reject them unless converted from a digit key above.
            if (name.Length == 0 || !Enum.GetNames(typeof(Key)).Contains(name, StringComparer.OrdinalIgnoreCase)
                || !Enum.TryParse<Key>(name, true, out var key)
                || !Enum.IsDefined(typeof(Key), key) || key == Key.None
                || new[] { Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LeftMeta, Key.RightMeta }.Contains(key))
                throw new InvalidDataException("Invalid shortcut key: " + name);
            return (key, modifiers.ToArray());
        }
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
}
