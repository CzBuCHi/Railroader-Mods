using System.Globalization;
using System;

namespace Tweaks.Units
{
    internal static class DistanceDisplay
    {
        internal static string? InMeters(float meters) => float.IsNaN(meters) || float.IsInfinity(meters) || meters < 0f
            ? null
            : meters >= 150f
                ? (Math.Floor(meters / 10d + 0.5d) / 100d).ToString("#,0.##", CultureInfo.CurrentCulture) + " km"
                : Math.Round(meters, MidpointRounding.ToEven).ToString("N0", CultureInfo.CurrentCulture) + " m";
    }
}
