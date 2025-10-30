namespace QuantLab.MarketData.Hub.Models.Config;

public sealed class DownloadServiceSettings
{
    public const string SectionName = "DownloadServiceSettings";
    public int MaxParallelWorkers { get; set; } = 3;
    public int MaxQueueSize { get; set; } = 100;
    public int BatchDelayMilliseconds { get; set; } = 1000;
}
