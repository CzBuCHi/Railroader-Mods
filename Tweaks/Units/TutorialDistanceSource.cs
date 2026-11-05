using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace Tweaks.Units
{
    internal static class TutorialDistanceSource
    {
        private static readonly Regex Functions = new Regex(
            @"^func (distanceString|distanceStringCeil)\(meters\) \{\r?\n.*?^\}",
            RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        internal static bool IsTutorialContext(string displayName, string streamingAssetsPath) =>
            !string.IsNullOrEmpty(displayName) && string.Equals(Path.GetFullPath(displayName),
                Path.GetFullPath(Path.Combine(streamingAssetsPath, "Tutorial", "pagecontext.rl")), StringComparison.OrdinalIgnoreCase);

        internal static string InMeters(string source)
        {
            var matches = Functions.Matches(source).Cast<Match>().ToArray();
            if (matches.Length != 2 || matches.Select(match => match.Groups[1].Value).Distinct().Count() != 2)
                throw new NotSupportedException("Tutorial distance helper functions have changed; keeping native script.");
            return Functions.Replace(source, match =>
            {
                var name = match.Groups[1].Value;
                var value = name == "distanceStringCeil" ? "math.ceil(meters)" : "math.floor(meters + 0.5)";
                var kilometers = name == "distanceStringCeil" ? "math.ceil(meters / 10) / 100" : "math.floor(meters / 10 + 0.5) / 100";
                return "func " + name + "(meters) {\r\n    if (meters >= 150) {\r\n        return \"\\(" + kilometers +
                    ") km\"\r\n    }\r\n    return \"\\(" + value + ") m\"\r\n}";
            });
        }
    }
}
