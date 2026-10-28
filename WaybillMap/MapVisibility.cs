using System;

namespace WaybillMap
{
    internal static class MapVisibility
    {
        internal static bool Show(int mode, bool engine, bool caboose, bool selected, int index, int stride)
        {
            if (engine) return true;
            if (mode == 0) return false;
            if (mode == 1) return caboose;
            return caboose || selected || index % Math.Max(1, stride) == 0;
        }
    }
}
