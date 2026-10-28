using System;

namespace WaybillMap
{
    internal static class MarkerStyle
    {
        internal const byte UnassignedChannel = 221; // #ddd

        internal static float DestinationBrightness(float r, float g, float b, bool spotted)
        {
            var peak = Math.Max(r, Math.Max(g, b));
            return peak <= 0.0001f ? 0 : (spotted ? 0.8f : 1f) / peak;
        }

    }
}
