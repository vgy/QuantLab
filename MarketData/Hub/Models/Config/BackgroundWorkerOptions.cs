namespace QuantLab.MarketData.Hub.Models.Config;

public sealed class BackgroundWorkerOptions
{
    public int MaxParallelWorkers { get; set; } = 3;
    public int MaxQueueSize { get; set; } = 100;
}
