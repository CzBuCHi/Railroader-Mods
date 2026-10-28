using System;
using System.Collections.Generic;

namespace WaybillMap
{
    internal static class TenderPairing
    {
        internal static Dictionary<T, T> Find<T>(IEnumerable<T> cars, Func<T, bool> needsTender,
            Func<T, T?> coupledAtRear, Func<T, bool> isTender) where T : class
        {
            var pairs = new Dictionary<T, T>();
            var claimed = new HashSet<T>();
            foreach (var engine in cars)
            {
                if (!needsTender(engine)) continue;
                var tender = coupledAtRear(engine);
                if (tender != null && isTender(tender) && claimed.Add(tender)) pairs.Add(engine, tender);
            }
            return pairs;
        }
    }
}
