using System;

namespace WaybillMap
{
    internal static class MapVisibility
    {
        internal static bool Show(int mode, bool engine, bool caboose, bool selected, int index, double averageLength, double pixelsPerMeter)
        {
            if (engine) return true;
            if (mode == 0) return false;
            if (mode == 1) return caboose;
            if (caboose || selected) return true;
            var spacing = Math.Max(0.001, averageLength * pixelsPerMeter);
            var stride = Math.Max(1, (int)Math.Min(100000, Math.Ceiling(averageLength * 2.4 / spacing)));
            return index % stride == 0;
        }
    }
}
