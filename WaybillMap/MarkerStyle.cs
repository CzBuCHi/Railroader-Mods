using System;

namespace WaybillMap
{
    internal static class MarkerStyle
    {
        internal const byte UnassignedChannel = 221; // #ddd

        internal static bool UseDarkText(double r, double g, double b, double alpha)
        {
            // Compare black/white contrast after compositing onto the dark map.
            r = r * alpha + 0.08 * (1 - alpha);
            g = g * alpha + 0.08 * (1 - alpha);
            b = b * alpha + 0.08 * (1 - alpha);
            var luminance = 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
            return (luminance + 0.05) / 0.05 >= 1.05 / (luminance + 0.05);
        }

        private static double Linear(double channel) => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
