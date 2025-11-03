namespace QuantLab.MarketData.Hub.Models.DTO.Grpc;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)] // for Primary Constructor
public readonly struct DecimalValue(long units, int scale)
{
    public readonly long Units = units;
    public readonly int Scale = scale;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal ToDecimal()
    {
        if (Scale == 0)
            return Units;
        decimal scaleFactor = 1m;
        for (int i = 0; i < Scale; i++)
            scaleFactor *= 0.1m;
        return Units * scaleFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecimalValue FromDecimal(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        int scale = (bits[3] >> 16) & 0x7F;
        bool sign = (bits[3] & 0x80000000) != 0;
        long lo = bits[0];
        long mid = ((long)bits[1]) << 32;
        long units = lo | mid;
        if (sign)
            units = -units;

        return new DecimalValue(units, scale);
    }
}
