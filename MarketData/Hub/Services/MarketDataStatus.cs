namespace QuantLab.MarketData.Hub.Services;

public record class MarketDataStatus
{
    public bool IsRunning { get; set; }
    public DateTime? LastFetchTime { get; set; }
    public string? LastError { get; set; }
    public string? LastResponseSnippet { get; set; }
    public int FetchCount { get; set; }
}