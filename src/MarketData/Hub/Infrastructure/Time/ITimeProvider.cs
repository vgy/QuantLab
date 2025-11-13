namespace QuantLab.MarketData.Hub.Infrastructure.Time;

public interface ITimeProvider
{
    DateTime Now { get; }
}
