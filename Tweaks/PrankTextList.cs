using System;
using System.Collections.Generic;

namespace Tweaks;

internal static class PrankTextList
{
    internal static string[] Parse(IEnumerable<string> lines)
    {
        var choices = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var text = line.Trim().TrimStart('\uFEFF');
            if (text.Length == 0) continue;
            // One file line is one choice; literal \n becomes a line break on the vehicle.
            text = text.Replace("\\n", "\n");
            // The native Customize window limits lettering to 100 characters.
            if (text.Length > 100) text = text.Substring(0, 100);
            if (seen.Add(text)) choices.Add(text);
        }
        return choices.ToArray();
    }
}
