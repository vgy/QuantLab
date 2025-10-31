namespace QuantLab.MarketData.Hub.Models.Domain;

public record struct Bar(
    string Symbol,
    BarInterval Interval,
    long Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    int Volume
);
