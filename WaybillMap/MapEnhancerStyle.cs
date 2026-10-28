using System;

namespace WaybillMap
{
    // Ported rendering constants from MapEnhancer (Michael Richer/Vanguard, MIT).
    internal static class MapEnhancerStyle
    {
        internal const float ChildScale = 0.8f;
        internal const float WagonLengthReference = 11f;
        internal const float FontSize = 19f;
        internal static float WagonScaleY(float length) => ChildScale * length / WagonLengthReference;
        internal static float EngineScaleY(float engineLength, float combinedLength) => ChildScale * combinedLength / engineLength;
        internal static string Label(string mark, string number, bool singleLine) => singleLine ? number : "<line-height=70%>" + mark + "\n" + number;
        internal static int SamplingStride(float nativeImageHeight, float zoom) =>
            // Native zoom is 0.2..8. Keep complete consists at yard/detail scales;
            // the original example deliberately accepts some neighboring overlap.
            zoom <= 1.2f ? 1 : Math.Max(1, (int)Math.Ceiling(nativeImageHeight * ChildScale / WagonLengthReference * zoom / 1.5f));
    }
}
