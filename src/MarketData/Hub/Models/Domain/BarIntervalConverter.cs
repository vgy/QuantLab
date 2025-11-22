using System.Runtime.CompilerServices;

namespace QuantLab.MarketData.Hub.Models.Domain;

public static class BarIntervalConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToShortString(this BarInterval interval) =>
        interval switch
        {
            BarInterval.FiveMinutes => "5min",
            BarInterval.FifteenMinutes => "15min",
            BarInterval.ThirtyMinutes => "30min",
            BarInterval.OneHour => "1h",
            BarInterval.OneDay => "1d",
            BarInterval.OneWeek => "1w",
            BarInterval.OneMonth => "1m",
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
        else if (s.Equals("5min".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.FiveMinutes;
            return true;
        }
        else if (s.Equals("15min".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.FifteenMinutes;
            return true;
        }
        else if (s.Equals("30min".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.ThirtyMinutes;
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
        else if (s.Equals("1w".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.OneWeek;
            return true;
        }
        else if (s.Equals("1m".AsSpan(), StringComparison.Ordinal))
        {
            barInterval = BarInterval.OneMonth;
            return true;
        }

        barInterval = default;
        return false;
    }
}
