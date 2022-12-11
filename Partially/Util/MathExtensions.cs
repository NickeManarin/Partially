using System;

namespace Partially.Util;

public static class MathExtensions
{
    public static double RoundUpValue(double value, int decimalpoint = 0)
    {
        var result = Math.Round(value, decimalpoint);

        if (result < value)
            result += Math.Pow(10, -decimalpoint);

        return result;
    }
}