using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WaybillMap
{
    internal sealed class SwitchProfileStore
    {
        private readonly string path;
        private static readonly Encoding Encoding = new UTF8Encoding(true);

        internal SwitchProfileStore(string path) { this.path = path; }

        internal SwitchSettings Load()
        {
            if (!File.Exists(path))
                return new SwitchSettings();
            var obj = JObject.Parse(File.ReadAllText(path));
            var token = obj[nameof(SwitchSettings.ThrownSwitches)];
            if (!(token is JArray ids) || ids.Any(id => id.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)id)))
                throw new InvalidDataException("Profile must contain a ThrownSwitches array of switch IDs: " + path);
            return new SwitchSettings { ThrownSwitches = ids.Values<string>().Distinct(StringComparer.Ordinal).ToArray()! };
        }

        internal void Save(SwitchSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var normalized = new SwitchSettings
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
