using System;

namespace SchrodingerServer.Common;

public class DecimalHelper
{
    public static int GetDecimalPlaces(decimal num)
    {
        var bits = decimal.GetBits(num);
        var scale = (bits[3] >> 16) & 0x7F;
        return scale;
    }

    public static long ConvertToLong(decimal num, int places)
    {
        var multiplier = (long)Math.Pow(10, places);
        return (long)(num * multiplier);
    }
}