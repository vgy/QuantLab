namespace QuantLab.MarketData.Hub.Services.Download;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Interface.Download;

public sealed class DownloadBackgroundService<T> : BackgroundService
{
    private readonly IDownloadQueue<T> _downloadQueue;
    private readonly ILogger<DownloadBackgroundService<T>> _logger;
    private readonly int _maxParallelWorkers;

    public DownloadBackgroundService(
        IDownloadQueue<T> downloadQueue,
        ILogger<DownloadBackgroundService<T>> logger,
        IOptions<MaxDownloadSettings> maxDownloadSettings
    )
    {
        _downloadQueue = downloadQueue;
        _logger = logger;
        _maxParallelWorkers = maxDownloadSettings.Value.MaxParallelWorkers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background worker service for {Type} started with {Count} workers",
            typeof(T).Name,
            _maxParallelWorkers
        );

        // Start multiple worker tasks
        var workerLoops = Enumerable
            .Range(0, _maxParallelWorkers)
            .Select(i => Task.Run(() => WorkerLoopAsync(i + 1, stoppingToken)))
            .ToArray();

        await Task.WhenAll(workerLoops); // Waits only when app stops
    }

    private async Task WorkerLoopAsync(int workerId, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (workItem, tcs) = await _downloadQueue.DequeueAsync(stoppingToken);

            try
            {
                _logger.LogInformation("👷 Worker {WorkerId} executing job...", workerId);
                var result = await workItem(stoppingToken);
                tcs.TrySetResult(result);
                _logger.LogInformation("✅ Worker {WorkerId} finished job.", workerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Worker {WorkerId} encountered an error", workerId);
                tcs.TrySetException(ex);
            }
        }
    }
}
