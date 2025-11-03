namespace QuantLab.MarketData.Hub.Models.Domain;

public readonly record struct Bar(
    string Symbol,
    BarInterval Interval,
    long Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    int Volume
);
