using System.Globalization;
using Tweaks.Tests;
using Tweaks.Units;

namespace Tweaks.Tests.Units
{
    internal static class TrainLengthTests
    {
        internal static void Run()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Program.Check(DistanceDisplay.InMeters(30.48f) == "30 m", "100 feet of distance displays as 30 meters");
                Program.Check(DistanceDisplay.InMeters(1609.344f) == "1.61 km", "A mile of distance displays in kilometers");
                Program.Check(DistanceDisplay.InMeters(149f) == "149 m" && DistanceDisplay.InMeters(150f) == "0.15 km" &&
                    DistanceDisplay.InMeters(1000f) == "1 km", "Distances switch to kilometers at 150 meters without redundant zeroes");
                Program.Check(DistanceDisplay.InMeters(0) == "0 m" && DistanceDisplay.InMeters(123.6f) == "124 m",
                    "Distances use whole meters including zero");
                Program.Check(DistanceDisplay.InMeters(float.NaN) == null && DistanceDisplay.InMeters(float.PositiveInfinity) == null &&
                    DistanceDisplay.InMeters(-1f) == null, "Invalid distances retain native formatter");
                Program.Check(TrainLengthDisplay.InMeters("10 cars, 1,234 tons, 999 ft", 304.6) == "10 cars, 1,234 tons, 305 m",
                    "Metric length preserves car count and tons and uses actual meters");
                Program.Check(TrainLengthDisplay.InMeters("1 car, 50 tons, 1 ft", 0) == "1 car, 50 tons, 0 m",
                    "Zero metric length is valid");
                Program.Check(TrainLengthDisplay.InMeters("10 cars, 500 tons, 4,051 ft", 1234.4).EndsWith("1,234 m"),
                    "Metric length respects current number grouping");
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
                Program.Check(DistanceDisplay.InMeters(1609.344f) == "1,61 km", "Distance formatting respects Czech locale");
                Program.Check(TrainLengthDisplay.InMeters("10 cars, 500 tons, 4 051 ft", 1234.4).EndsWith(1234.ToString("N0") + " m"),
                    "Czech grouping is preserved in metric display");
                foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -1d })
                    Program.Check(TrainLengthDisplay.InMeters("1 car, 50 tons, 5 ft", invalid) == "1 car, 50 tons, 5 ft",
                        "Invalid metric length retains native summary");
                Program.Check(TrainLengthDisplay.InMeters("Changed native format", 10) == "Changed native format" &&
                    TrainLengthDisplay.InMeters("1 car, 50 tons, 10 m", 10) == "1 car, 50 tons, 10 m",
                    "Unknown/already-metric text is left intact");
            }
            finally { CultureInfo.CurrentCulture = saved; }
        }
    }
}
