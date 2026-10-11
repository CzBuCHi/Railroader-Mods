using System;

namespace Tweaks;

internal static class ShopWorkTiming
{
    internal const float Duration = 300f;
    internal static float Remaining(float remaining, double last, double now, bool stopped)
        => stopped ? (float)Math.Max(0, remaining - Math.Max(0, now - last)) : remaining;
}
