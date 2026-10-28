using System;

namespace WaybillMap
{
    internal readonly struct MarkerGeometry
    {
        internal const int BatchSize = 2048; // At most ten vertices per outlined marker, below Unity UI's 16-bit limit.
        internal readonly double X, Y, AxisX, AxisY, HalfLength, HalfWidth;

        private MarkerGeometry(double x, double y, double axisX, double axisY, double length, double width)
        { X = x; Y = y; AxisX = axisX; AxisY = axisY; HalfLength = length; HalfWidth = width; }

        internal static bool TryCreate(double ax, double ay, double bx, double by,
            double uvX, double uvY, double uvWidth, double uvHeight,
            double left, double bottom, double width, double height, out MarkerGeometry marker, double carLength = 12,
            double scale = 1, double lengthScale = 1)
        {
            marker = default;
            if (!Finite(ax) || !Finite(ay) || !Finite(bx) || !Finite(by) || !Finite(uvWidth) || !Finite(uvHeight) ||
                !Finite(uvX) || !Finite(uvY) || !Finite(left) || !Finite(bottom) || !Finite(width) || !Finite(height) ||
                Math.Abs(uvWidth) < 1e-9 || Math.Abs(uvHeight) < 1e-9 || width <= 0 || height <= 0 || !Finite(carLength) || carLength <= 0 ||
                !Finite(scale) || !Finite(lengthScale) || scale <= 0 || lengthScale <= 0) return false;
            ax = left + (ax - uvX) / uvWidth * width;
            bx = left + (bx - uvX) / uvWidth * width;
            ay = bottom + (ay - uvY) / uvHeight * height;
            by = bottom + (by - uvY) / uvHeight * height;
            var dx = bx - ax; var dy = by - ay;
            var length = Math.Sqrt(dx * dx + dy * dy);
            var x = (ax + bx) * 0.5; var y = (ay + by) * 0.5;
            var axisX = length > 1e-6 ? dx / length : 1;
            var axisY = length > 1e-6 ? dy / length : 0;
            // Keep the overview baseline, widening in step with thick native track lines.
            var halfLength = Math.Max(7.2, carLength * 1.2 * lengthScale) * scale;
            var halfWidth = 7.2 * scale;
            var extentX = Math.Abs(axisX) * (halfLength + 0.75) + Math.Abs(axisY) * (halfWidth + 0.75);
            var extentY = Math.Abs(axisY) * (halfLength + 0.75) + Math.Abs(axisX) * (halfWidth + 0.75);
            if (x + extentX < left || x - extentX > left + width || y + extentY < bottom || y - extentY > bottom + height)
                return false;
            marker = new MarkerGeometry(x, y, axisX, axisY, halfLength, halfWidth);
            return true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal double ReadableAngle
        {
            get
            {
                var angle = Math.Atan2(AxisY, AxisX) * 180 / Math.PI;
                if (angle > 90) angle -= 180;
                if (angle < -90) angle += 180;
                return angle;
            }
        }
    }
}
