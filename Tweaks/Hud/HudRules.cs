using System.Collections.Generic;
using System.Linq;
using System;
using Settings = global::Tweaks.Configuration.Settings;

namespace Tweaks.Hud
{
    internal static class HudRules
    {
        internal static float OilWarningThreshold => Settings.Current.OilWarningPercent / 100f;
        internal static bool CanFillOil(float oil, float speedMph) =>
            !float.IsNaN(oil) && !float.IsInfinity(oil) && oil < 1f && !float.IsNaN(speedMph) && Math.Abs(speedMph) <= 5f;
        internal static int HighlightedMode(int nativeValue) => nativeValue >= 0 && nativeValue < 3 ? nativeValue : -1;
        internal static bool NeedsAir(bool hasSet, bool coupled, bool airConnected, float anglecock) =>
            hasSet && (coupled ? !airConnected || anglecock < 0.999f : anglecock > 0.001f);
        internal static T? FindWarning<T>(IEnumerable<T> cars, Func<T, bool> oilEnabled, Func<T, bool> hotbox, Func<T, float> oil) where T : class =>
            cars.Where(car => oilEnabled(car) && (hotbox(car) || oil(car) < OilWarningThreshold))
                .OrderByDescending(hotbox).ThenBy(oil).FirstOrDefault();
    }
}
