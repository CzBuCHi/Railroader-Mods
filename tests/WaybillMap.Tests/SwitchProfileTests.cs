using System;
using System.IO;
using System.Linq;
using System.Text;
using WaybillMap;

internal static class SwitchProfileTests
{
    internal static void Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "WaybillMap.SwitchProfiles.Tests-" + Guid.NewGuid());
        var path = Path.Combine(folder, "profile.json");
        Directory.CreateDirectory(folder);
        try
        {
            var store = new SwitchProfileStore(path);
            Program.Check(store.Load().ThrownSwitches.Length == 0, "Missing profile normalizes all switches");
            store.Save(new SwitchSettings { ThrownSwitches = new[] { "B", "A", "B", "", " " } });
            Program.Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "A", "B" }), "Only unique, nonempty reversed IDs are saved");
            var bytes = File.ReadAllBytes(path);
            Program.Check(bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "UTF-8 BOM");
            Program.Check(!File.ReadAllText(path).Replace("\r\n", "").Contains("\n"), "CRLF");
            store.Save(new SwitchSettings { ThrownSwitches = new[] { "C" } });
            Program.Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "C" }), "Save replaces old profile");
            Program.Check(new SwitchProfileStore(path + ".bak").Load().ThrownSwitches.SequenceEqual(new[] { "A", "B" }), "Previous profile is backed up");
            Program.Check(!File.Exists(path + ".tmp"), "No temporary file left after save");
            store.Save(new SwitchSettings());
            Program.Check(store.Load().ThrownSwitches.Length == 0, "Empty profile is valid");
            foreach (var corrupt in new[] { "{", "{}", "null", "{\"ThrownSwitches\":null}", "{\"ThrownSwitches\":[1]}", "{\"ThrownSwitches\":[\"\"]}" })
            {
                File.WriteAllText(path, corrupt, new UTF8Encoding(true));
                var rejected = false;
                try { store.Load(); }
                catch (Exception) { rejected = true; }
                Program.Check(rejected, "Corrupt profile must abort restoration: " + corrupt);
                Program.Check(File.ReadAllText(path) == corrupt, "Loading must not overwrite corrupt data");
            }
            // A failed atomic replace must retain the existing file.
            store.Save(new SwitchSettings { ThrownSwitches = new[] { "original" } });
            using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var rejected = false;
                try { store.Save(new SwitchSettings { ThrownSwitches = new[] { "replacement" } }); }
                catch (IOException) { rejected = true; }
                Program.Check(rejected, "Failed replace is reported");
            }
            Program.Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "original" }), "Failed save preserves original profile");
            Program.Check(!File.Exists(path + ".tmp"), "Failed save cleans temporary file");

        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
