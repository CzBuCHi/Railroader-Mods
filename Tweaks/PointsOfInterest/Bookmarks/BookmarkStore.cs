using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Tweaks.PointsOfInterest.Configuration;
using Settings = global::Tweaks.PointsOfInterest.Configuration.Settings;

namespace Tweaks.PointsOfInterest.Bookmarks
{
    internal sealed class BookmarkStore
    {
        private readonly string path;
        internal Settings Current { get; private set; }

        internal BookmarkStore(string path)
        {
            this.path = path;
            if (File.Exists(path)) Current = Read(path);
            else Current = new Settings();
        }

        private static Settings Read(string path)
        {
            var json = JObject.Parse(File.ReadAllText(path));
            return global::Tweaks.Configuration.Settings.Parse(json).PointsOfInterest;
        }

        internal static void ValidateJson(JObject json)
        {
            foreach (var property in json.Properties())
            {
                if (property.Name == nameof(Settings.AutoOpenWindow) && property.Value.Type == JTokenType.Boolean) continue;
                if (property.Name == nameof(Settings.OpenWindowShortcut) && property.Value.Type == JTokenType.String) continue;
                if (property.Name == nameof(Settings.Groups) && property.Value.Type == JTokenType.Array) continue;
                throw new InvalidDataException("Invalid PointsOfInterest setting: " + property.Name);
            }
        }

        internal static void Validate(Settings settings)
        {
            settings.ParseOpenWindowShortcut();
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

        internal void Rename(string groupName, string name, string targetGroup, string targetName)
        {
            targetGroup = targetGroup.Trim(); targetName = targetName.Trim();
            RequireName(targetGroup); RequireName(targetName);
            Change(settings =>
            {
                var source = settings.Groups.Single(group => group.Name == groupName);
                if (!source.Locations.TryGetValue(name, out var location))
                    throw new InvalidOperationException("Select a saved location.");
                if (groupName == targetGroup && name == targetName) return;
                var destination = settings.Groups.FirstOrDefault(group => group.Name == targetGroup);
                if (destination != null && destination.Locations.ContainsKey(targetName))
                    throw new InvalidOperationException("This location already exists in the target group.");
                if (destination == null)
                {
                    destination = new LocationGroup { Name = targetGroup };
                    settings.Groups.Add(destination);
                }
                source.Locations.Remove(name);
                destination.Locations.Add(targetName, location);
            });
        }

        internal void MoveGroup(string groupName, int delta) => Change(settings =>
        {
            if (delta != -1 && delta != 1) throw new ArgumentOutOfRangeException(nameof(delta));
            var index = settings.Groups.FindIndex(group => group.Name == groupName);
            var next = index + delta;
            if (index < 0 || next < 0 || next >= settings.Groups.Count) return;
            var group = settings.Groups[index];
            settings.Groups.RemoveAt(index); settings.Groups.Insert(next, group);
        });

        internal void SetAutoOpen(bool enabled) => Change(settings => settings.AutoOpenWindow = enabled);

        private void Change(Action<Settings> action)
        {
            var next = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(Current))!;
            action(next);
            Validate(next);
            Write(next);
            Current = next;
            global::Tweaks.Configuration.Settings.Current.PointsOfInterest = next;
        }

        private void Write(Settings settings) =>
            global::Tweaks.Configuration.Settings.WriteSection(path, "PointsOfInterest", JObject.FromObject(settings));
    }
}
