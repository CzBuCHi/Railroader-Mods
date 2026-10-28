using System;
using System.Globalization;

namespace Tweaks
{
    internal static class DistanceDisplay
    {
        internal static string? InMeters(float meters) => float.IsNaN(meters) || float.IsInfinity(meters) || meters < 0f
            ? null
            : Math.Round(meters, MidpointRounding.ToEven).ToString("N0", CultureInfo.CurrentCulture) + " m";
    }
}
