using System;
using System.Windows;

namespace Partially.Util;

public static class PointExtensions
{
    public static Point Scale(this Point point, double scale)
    {
        return new Point(Math.Round(point.X * scale, MidpointRounding.AwayFromZero), Math.Round(point.Y * scale, MidpointRounding.AwayFromZero));
    }
}