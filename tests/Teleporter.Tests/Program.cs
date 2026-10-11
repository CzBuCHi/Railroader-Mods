using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Teleporter;

var path = args.Length == 1 ? args[0] : throw new ArgumentException("Pass the legacy settings JSON path.");
var text = File.ReadAllText(path);
var settings = JsonConvert.DeserializeObject<Settings>(text) ?? throw new Exception("Missing settings.");
var legacy = JObject.Parse(text);
var groups = (JArray)legacy["Groups"]!;
Check(groups.Count == settings.Groups.Count, "Group count");
var count = 0;
for (var i = 0; i < groups.Count; i++)
{
    var group = settings.Groups[i];
    Check(group.Name == (string?)groups[i]["Name"], "Group name and order");
    var locations = (JObject)groups[i]["Locations"]!;
    Check(locations.Count == group.Locations.Count, "Location count");
    foreach (var pair in locations.Properties())
    {
        Check(group.Locations.TryGetValue(pair.Name, out var location), "Location name");
        var value = location!;
        Check(!value.FirstPerson, "Legacy defaults to tracking camera");
        Check(value.Distance == (float)pair.Value["Distance"]!, "Zoom");
        CheckVector(value.Position, pair.Value["Position"]!);
        CheckVector(value.Rotation, pair.Value["Rotation"]!);
        count++;
    }
}
Check(settings.AutoOpenTeleporterWindow == (bool)legacy["AutoOpenTeleporterWindow"]!, "Auto-open preference");
var serialized = JsonConvert.SerializeObject(settings);
var restored = JsonConvert.DeserializeObject<Settings>(serialized)!;
Check(JToken.DeepEquals(JToken.Parse(serialized), JToken.Parse(JsonConvert.SerializeObject(restored))), "Round trip");
var firstPerson = new TeleportLocation { FirstPerson = true, Position = new Vector { X = 10, Y = 20, Z = 30 }, Rotation = new Vector { X = 350, Y = 135, Z = 0 } };
var restoredPerson = JsonConvert.DeserializeObject<TeleportLocation>(JsonConvert.SerializeObject(firstPerson))!;
Check(restoredPerson.FirstPerson && restoredPerson.Position.Y == 20 && restoredPerson.Rotation.X == 350, "First-person data round trip");
Console.WriteLine($"PASS: {groups.Count} legacy groups, {count} camera views, group order, all coordinates, rotations, zooms, preference and JSON round trip. Original file was read only.");

static void CheckVector(Vector vector, JToken token)
{
    Check(vector.X == (float)token["X"]! && vector.Y == (float)token["Y"]! && vector.Z == (float)token["Z"]!, "Vector coordinates");
}
static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); }