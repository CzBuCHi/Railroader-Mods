using System;

namespace WaybillMap
{
    internal readonly struct MarkerSpacing
    {
        internal readonly int Stride;
        internal readonly double LengthScale;
        internal MarkerSpacing(int stride, double lengthScale) { Stride = stride; LengthScale = lengthScale; }
    }

    internal static class MarkerLayout
    {
        // Roughly half the 12-screen-pixel gap in the reference, at its ~0.8 UI scale.
        internal const double Gap = 8;
        internal static double ScaleForTrack(double trackWidth) =>
            double.IsNaN(trackWidth) || double.IsInfinity(trackWidth) ? 1 : Math.Max(1, trackWidth * 6 / 14.4);

        internal static MarkerSpacing ForConsist(double averageLength, double pixelsPerMeter, double scale)
        {
            var spacing = Math.Max(0.001, averageLength * pixelsPerMeter);
            var desiredLength = Math.Max(14.4, averageLength * 2.4) * scale;
            var stride = (int)Math.Min(100000, Math.Max(1, Math.Round((desiredLength + Gap) / spacing, MidpointRounding.AwayFromZero)));
            stride = (int)Math.Min(100000, Math.Max(stride, Math.Ceiling((14.4 * scale + Gap) / spacing)));
            var length = Math.Max(14.4 * scale, stride * spacing - Gap);
            return new MarkerSpacing(stride, length / desiredLength);
        }
    }
}
