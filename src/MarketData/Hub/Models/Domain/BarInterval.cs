namespace QuantLab.MarketData.Hub.Models.Domain;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public sealed record BarInterval
{
    public int Minutes { get; init; }
    public string Symbol { get; init; } = "";

    private BarInterval(int minutes, string symbol)
    {
        Minutes = minutes;
        Symbol = symbol;
    }

    public static readonly BarInterval OneMinute = new(1, "1m");
    public static readonly BarInterval FiveMinutes = new(5, "5m");
    public static readonly BarInterval FifteenMinutes = new(15, "15m");
    public static readonly BarInterval ThirtyMinutes = new(30, "30m");
    public static readonly BarInterval OneHour = new(60, "1h");
    public static readonly BarInterval FourHours = new(240, "4h");
    public static readonly BarInterval SixHours = new(360, "6h");
    public static readonly BarInterval OneDay = new(1440, "1d");
    public static readonly BarInterval OneWeek = new(10080, "1w");

    private static readonly IReadOnlyList<BarInterval> _all = new ReadOnlyCollection<BarInterval>(
        [
            OneMinute,
            FiveMinutes,
            FifteenMinutes,
            ThirtyMinutes,
            OneHour,
            FourHours,
            SixHours,
            OneDay,
            OneWeek,
        ]
    );

    public static IReadOnlyList<BarInterval> All => _all;

    private static readonly Dictionary<string, BarInterval> _bySymbol = _all.ToDictionary(
        x => x.Symbol,
        StringComparer.OrdinalIgnoreCase
    );

    public static bool TryParse(string? symbol, out BarInterval? interval)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            interval = null;
            return false;
        }

        return _bySymbol.TryGetValue(symbol, out interval);
    }

    public override string ToString() => Symbol;
}
