using System;

namespace Utils;

public static class Rounding
{
    public static double? ToHalf(double? value) 
    {
        return value == null ? null : Math.Round(value.Value * 2) / 2;
    }
}