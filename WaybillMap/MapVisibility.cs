using System;

namespace WaybillMap
{
    internal static class MapVisibility
    {
        internal static bool Show(int mode, bool engine, bool caboose, bool selected, int index, double averageLength, double pixelsPerMeter, double scale = 1)
        {
            if (engine) return true;
            if (mode == 0) return false;
            if (mode == 1) return caboose;
            if (caboose || selected) return true;
            return index % MarkerLayout.ForConsist(averageLength, pixelsPerMeter, scale).Stride == 0;
        }
    }
}
