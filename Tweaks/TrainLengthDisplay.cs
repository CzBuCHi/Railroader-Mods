using System;
using System.Globalization;

namespace Tweaks
{
    internal static class TrainLengthDisplay
    {
        internal static string InMeters(string summary, double meters)
        {
            var separator = summary.LastIndexOf(", ", StringComparison.Ordinal);
            if (separator < 0 || !summary.EndsWith(" ft", StringComparison.Ordinal) ||
                double.IsNaN(meters) || double.IsInfinity(meters) || meters < 0) return summary;
            return summary.Substring(0, separator + 2) +
                Math.Round(meters, MidpointRounding.ToEven).ToString("N0", CultureInfo.CurrentCulture) + " m";
        }
    }
}
