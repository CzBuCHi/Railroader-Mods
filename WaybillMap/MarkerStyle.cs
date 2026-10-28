using System;

namespace WaybillMap
{
    internal static class MarkerStyle
    {
        internal const byte UnassignedChannel = 221; // #ddd

        internal static float DestinationBrightness(float r, float g, float b, bool spotted)
        {
            var peak = Math.Max(r, Math.Max(g, b));
            return peak <= 0.0001f ? 0 : (spotted ? 0.6f : 1f) / peak;
        }

        internal static float DestinationChannel(float channel, float brightness, bool spotted) =>
            spotted ? channel * brightness : channel * brightness * 0.8f + 0.2f;

    }
}
