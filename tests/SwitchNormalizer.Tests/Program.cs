using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using SwitchNormalizer;

internal static class Program
{
    private static int checks;

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    private static void Main(string[] args)
    {
        var folder = Path.Combine(Path.GetTempPath(), "SwitchNormalizer.Tests-" + Guid.NewGuid());
        var path = Path.Combine(folder, "profile.json");
        Directory.CreateDirectory(folder);
        try
        {
            var store = new ProfileStore(path);
            Check(store.Load().ThrownSwitches.Length == 0, "Missing profile normalizes all switches");
            store.Save(new Settings { ThrownSwitches = new[] { "B", "A", "B", "", " " } });
            Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "A", "B" }), "Only unique, nonempty reversed IDs are saved");
            var bytes = File.ReadAllBytes(path);
            Check(bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 }), "UTF-8 BOM");
            Check(!File.ReadAllText(path).Replace("\r\n", "").Contains("\n"), "CRLF");
            store.Save(new Settings { ThrownSwitches = new[] { "C" } });
            Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "C" }), "Save replaces old profile");
            Check(new ProfileStore(path + ".bak").Load().ThrownSwitches.SequenceEqual(new[] { "A", "B" }), "Previous profile is backed up");
            Check(!File.Exists(path + ".tmp"), "No temporary file left after save");
            store.Save(new Settings());
            Check(store.Load().ThrownSwitches.Length == 0, "Empty profile is valid");
            foreach (var corrupt in new[] { "{", "{}", "null", "{\"ThrownSwitches\":null}", "{\"ThrownSwitches\":[1]}", "{\"ThrownSwitches\":[\"\"]}" })
            {
                File.WriteAllText(path, corrupt, new UTF8Encoding(true));
                var rejected = false;
                try { store.Load(); }
                catch (Exception) { rejected = true; }
                Check(rejected, "Corrupt profile must abort restoration: " + corrupt);
                Check(File.ReadAllText(path) == corrupt, "Loading must not overwrite corrupt data");
            }
            // A failed atomic replace must retain the existing file.
            store.Save(new Settings { ThrownSwitches = new[] { "original" } });
            using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var rejected = false;
                try { store.Save(new Settings { ThrownSwitches = new[] { "replacement" } }); }
                catch (IOException) { rejected = true; }
                Check(rejected, "Failed replace is reported");
            }
            Check(store.Load().ThrownSwitches.SequenceEqual(new[] { "original" }), "Failed save preserves original profile");
            Check(!File.Exists(path + ".tmp"), "Failed save cleans temporary file");

            using var stream = File.OpenRead(args[0]);
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            var references = metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name)).ToArray();
            Check(references.Contains("Railroader.Modding"), "Native modding contract referenced");
            Check(!references.Any(name => name.Contains("RailForge") || name.Contains("UnityModManager") || name.Contains("Harmony")), "No third-party loader or patch dependency");
            var entry = metadata.TypeDefinitions.Select(handle => metadata.GetTypeDefinition(handle))
                .Single(type => metadata.GetString(type.Name) == "SwitchNormalizerPlugin");
            var interfaces = entry.GetInterfaceImplementations().Select(handle => metadata.GetInterfaceImplementation(handle).Interface)
                .Select(handle => metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Name)).ToArray();
            Check(interfaces.Contains("IModEntryPoint") && interfaces.Contains("IWorldLifecycle"), "Loader and world lifecycle interfaces present");
            Check(metadata.ManifestResources.Any(handle => metadata.GetString(metadata.GetManifestResource(handle).Name) == "SwitchNormalizer.icon.png"), "Toolbar icon embedded");
            Console.WriteLine("PASS: " + checks + " profile persistence and compiled mod contract checks.");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
