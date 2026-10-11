using System;
using System.Collections.Generic;

namespace Tweaks;

internal static class NextServiceSelection
{
    internal static double? Find(double now, IEnumerable<double> services)
    {
        double? earliest = null;
        foreach (var service in services)
        {
            if (double.IsNaN(service) || double.IsInfinity(service)) continue;
            // An overdue native service is due immediately, not tomorrow.
            var target = Math.Max(service, now + 1.0);
            if (!earliest.HasValue || target < earliest.Value) earliest = target;
        }
        return earliest;
    }
}