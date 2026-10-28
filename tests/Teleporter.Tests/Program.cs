using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Relay;
using Teleporter;

internal static class Program
{
    internal static int Checks;
    internal static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Checks++;
    }

    internal static TeleportLocation View(float x = 1, bool firstPerson = false) => new TeleportLocation
    {
        Position = new Vector { X = x, Y = 2, Z = 3 },
        Rotation = new Vector { X = 20, Y = 135 }, Distance = 40, FirstPerson = firstPerson
    };

    private static void Main(string[] args)
    {
        var folder = Path.Combine(Path.GetTempPath(), "Teleporter.Tests-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "profile.json");
            var store = new BookmarkStore(path);
            Check(store.Current.Groups.Count == 0, "Empty profile");
            store.Add(" Yard ", " View A ", View());
            Check(store.Get("Yard", "View A").Rotation.Y == 135, "Camera orientation retained");
            var failed = false;
            try { store.Add("Yard", "View A", View(9)); } catch (InvalidOperationException) { failed = true; }
            Check(failed && store.Get("Yard", "View A").Position.X == 1, "Duplicate add preserves existing view");
            store.Replace("Yard", "View A", View(4, true));
            Check(new BookmarkStore(path).Get("Yard", "View A").FirstPerson, "First-person mode survives reload");
            Check(new BookmarkStore(path).Get("Yard", "View A").Position.X == 4, "Replacement persists");
            store.Add("Station", "View B", View());
            store.MoveGroup("Station", -1);
            Check(store.Current.Groups[0].Name == "Station", "Group reordering persists");
            store.SetAutoOpen(true);
            Check(new BookmarkStore(path).Current.AutoOpenTeleporterWindow, "Auto-open persists");
            store.Delete("Yard", "View A");
            Check(store.Current.Groups.Single(g => g.Name == "Yard").Locations.Count == 0, "Delete keeps group");
            var invalid = View(); invalid.Position.X = float.NaN;
            failed = false;
            try { store.Add("Yard", "Bad", invalid); } catch (InvalidDataException) { failed = true; }
            Check(failed && !store.Current.Groups.Single(g => g.Name == "Yard").Locations.ContainsKey("Bad"), "Invalid camera data does not mutate profile");
            using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                failed = false;
                try { store.Add("Yard", "Locked", View()); } catch (IOException) { failed = true; }
                Check(failed, "Failed disk write is reported");
            }
            Check(!store.Current.Groups.Single(g => g.Name == "Yard").Locations.ContainsKey("Locked"), "Failed save does not commit in-memory changes");
            Check(!File.Exists(path + ".tmp"), "Temporary file cleaned");
            Check(File.Exists(path + ".bak"), "Previous profile backed up");
            Check(File.ReadAllBytes(path).Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "UTF-8 BOM");
            Check(!File.ReadAllText(path).Replace("\r\n", "").Contains("\n"), "CRLF profile");
            var missing = Path.Combine(folder, "missing.json");
            Check(new BookmarkStore(missing).Current.Groups.Count == 0 && !File.Exists(missing),
                "Loading a missing profile does not import or create files");
            foreach (var text in new[] { "{", "{}", "{\"Groups\":null}", "{\"Groups\":[null]}", "{\"Groups\":[{\"Name\":\"X\",\"Locations\":null}]}" })
            {
                File.WriteAllText(path, text);
                failed = false;
                try { new BookmarkStore(path); } catch (Exception) { failed = true; }
                Check(failed && File.ReadAllText(path) == text, "Malformed profile rejected without overwrite");
            }
            RelayUiTests.Run(File.ReadAllText(args[0]), new BookmarkStore(Path.Combine(folder, "relay.json")));
            InstalledApiTests.Run(args[1], args[2]);
            Console.WriteLine("PASS: " + Checks + " storage, actual Relay VM UI, and installed API contract checks.");
        }
        finally { Directory.Delete(folder, true); }
    }
}
