using System.Runtime.CompilerServices;

namespace QuantLab.MarketData.Hub.Models.Domain;

public static class BarIntervalConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToShortString(this BarInterval interval) =>
        interval switch
        {
            BarInterval.FiveMinutes => "5m",
            BarInterval.OneHour => "1h",
            BarInterval.OneDay => "1d",
            _ => "5m",
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, out BarInterval barInterval)
    {
        if (s.IsEmpty)
        {
            barInterval = default;
            return false;
        }
        else if (s.Equals("5m".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.FiveMinutes;
            return true;
        }
        else if (s.Equals("1h".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.OneHour;
            return true;
        }
        else if (s.Equals("1d".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.OneDay;
            return true;
        }

        barInterval = default;
        return false;
    }
}
