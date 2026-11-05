using System;

namespace Tweaks.MapTweaks.Rendering
{
    internal static class VehicleMarkerGeometry
    {
        // Match the visible native locomotive width, accounting for trimmed sprite padding.
        internal static float Width(float nativeWidth, float zoom, float visibleFraction) => nativeWidth * zoom * visibleFraction;
        // Retain a small length floor for overview visibility.
        internal static float Length(float carLength, float metersPerPixel) => Math.Max(carLength, 3f * metersPerPixel);
        internal static string Label(bool locomotive, string number) => locomotive ? number : "";
        internal static int SamplingStride(float carLength, float metersPerPixel, float zoom)
        {
            if (zoom <= 1.2f || metersPerPixel <= 0 || carLength <= 0) return 1;
            return Math.Max(1, (int)Math.Ceiling(4f * metersPerPixel / carLength));
        }
    }
}
