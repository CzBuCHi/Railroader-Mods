using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Teleporter
{
    internal sealed class BookmarkStore
    {
        private readonly string path;
        internal Settings Current { get; private set; }

        internal BookmarkStore(string path, string? legacyPath = null)
        {
            this.path = path;
            if (File.Exists(path)) Current = Read(path);
            else if (legacyPath != null && File.Exists(legacyPath))
            {
                Current = Read(legacyPath);
                Write(Current);
            }
            else Current = new Settings();
        }

        private static Settings Read(string path)
        {
            var json = JObject.Parse(File.ReadAllText(path));
            if (!(json["Groups"] is JArray)) throw new InvalidDataException("Teleporter profile has no Groups array.");
            var settings = json.ToObject<Settings>() ?? throw new InvalidDataException("Empty Teleporter profile.");
            Validate(settings);
            return settings;
        }

        internal static void Validate(Settings settings)
        {
            if (settings.Groups == null || settings.Groups.Any(group => group == null))
                throw new InvalidDataException("Invalid location groups.");
            if (settings.Groups.Select(group => group.Name).Distinct(StringComparer.Ordinal).Count() != settings.Groups.Count)
                throw new InvalidDataException("Duplicate location groups.");
            foreach (var group in settings.Groups)
            {
                RequireName(group.Name);
                if (group.Locations == null) throw new InvalidDataException("Missing location list.");
                foreach (var pair in group.Locations)
                {
                    RequireName(pair.Key);
                    var location = pair.Value;
                    if (location == null || location.Position == null || location.Rotation == null ||
                        !Finite(location.Position.X, location.Position.Y, location.Position.Z,
                            location.Rotation.X, location.Rotation.Y, location.Rotation.Z, location.Distance) ||
                        (!location.FirstPerson && location.Distance < 0))
                        throw new InvalidDataException("Invalid saved camera view: " + pair.Key);
                }
            }
        }

        private static bool Finite(params float[] values) => values.All(value => !float.IsNaN(value) && !float.IsInfinity(value));
        private static void RequireName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter a group name and a location name.");
        }

        internal TeleportLocation Get(string group, string name) =>
            Current.Groups.Single(item => item.Name == group).Locations[name];

        internal void Add(string groupName, string name, TeleportLocation location)
        {
            groupName = groupName.Trim(); name = name.Trim();
            RequireName(groupName); RequireName(name);
            Change(settings =>
            {
                var group = settings.Groups.FirstOrDefault(item => item.Name == groupName);
                if (group == null) { group = new LocationGroup { Name = groupName }; settings.Groups.Add(group); }
                if (group.Locations.ContainsKey(name)) throw new InvalidOperationException("This location already exists. Use Replace.");
                group.Locations.Add(name, location);
            });
        }

        internal void Replace(string groupName, string name, TeleportLocation location) => Change(settings =>
        {
            var group = settings.Groups.Single(item => item.Name == groupName);
            if (!group.Locations.ContainsKey(name)) throw new InvalidOperationException("Select a saved location.");
            group.Locations[name] = location;
        });

        internal void Delete(string groupName, string name) => Change(settings =>
        {
            if (!settings.Groups.Single(item => item.Name == groupName).Locations.Remove(name))
                throw new InvalidOperationException("Select a saved location.");
        });

        internal void MoveGroup(string groupName, int delta) => Change(settings =>
        {
            if (delta != -1 && delta != 1) throw new ArgumentOutOfRangeException(nameof(delta));
            var index = settings.Groups.FindIndex(group => group.Name == groupName);
            var next = index + delta;
            if (index < 0 || next < 0 || next >= settings.Groups.Count) return;
            var group = settings.Groups[index];
            settings.Groups.RemoveAt(index); settings.Groups.Insert(next, group);
        });

        internal void SetAutoOpen(bool enabled) => Change(settings => settings.AutoOpenTeleporterWindow = enabled);

        private void Change(Action<Settings> action)
        {
            var next = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(Current))!;
            action(next);
            Validate(next);
            Write(next);
            Current = next;
        }

        private void Write(Settings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            try
            {
                var text = JsonConvert.SerializeObject(settings, Formatting.Indented)
                    .Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
                File.WriteAllText(temporary, text, new UTF8Encoding(true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
