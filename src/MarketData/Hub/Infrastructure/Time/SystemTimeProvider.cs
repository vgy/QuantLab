namespace QuantLab.MarketData.Hub.Infrastructure.Time;

public class SystemTimeProvider(string? timeZoneId = null) : ITimeProvider
{
    private readonly TimeZoneInfo _timeZone = string.IsNullOrEmpty(timeZoneId)
        ? TimeZoneInfo.Local
        : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
}
