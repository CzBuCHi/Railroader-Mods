using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SwitchNormalizer
{
    internal sealed class ProfileStore
    {
        private readonly string path;
        private static readonly Encoding Encoding = new UTF8Encoding(true);

        internal ProfileStore(string path) { this.path = path; }

        internal Settings Load()
        {
            if (!File.Exists(path))
                return new Settings();
            var obj = JObject.Parse(File.ReadAllText(path));
            var token = obj[nameof(Settings.ThrownSwitches)];
            if (!(token is JArray ids) || ids.Any(id => id.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)id)))
                throw new InvalidDataException("Profile must contain a ThrownSwitches array of switch IDs: " + path);
            return new Settings { ThrownSwitches = ids.Values<string>().Distinct(StringComparer.Ordinal).ToArray()! };
        }

        internal void Save(Settings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var normalized = new Settings
            {
                ThrownSwitches = settings.ThrownSwitches.Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray()
            };
            var text = JsonConvert.SerializeObject(normalized, Formatting.Indented)
                .Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
            var temporary = path + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, Encoding);
                if (File.Exists(path))
                    File.Replace(temporary, path, path + ".bak");
                else
                    File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }
}
