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
    private readonly IOptionsMonitor<DownloadServiceSettings> _downloadServiceSettingsMonitor;
    private readonly int _maxParallelWorkers;
    private int _currentBatchCount = 0;
    private TaskCompletionSource _batchTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly object _batchLock = new();

    public DownloadBackgroundService(
        IDownloadQueue<T> downloadQueue,
        ILogger<DownloadBackgroundService<T>> logger,
        IOptionsMonitor<DownloadServiceSettings> downloadServiceSettingsMonitor
    )
    {
        _downloadQueue = downloadQueue;
        _logger = logger;
        _downloadServiceSettingsMonitor = downloadServiceSettingsMonitor;
        _maxParallelWorkers = _downloadServiceSettingsMonitor.CurrentValue.MaxParallelWorkers;
        _batchTcs.SetResult(); // First batch starts immediately
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

            // ===== Batch throttling BEFORE starting work =====
            TaskCompletionSource tcsToAwait;
            lock (_batchLock)
            {
                _currentBatchCount++;
                tcsToAwait = _batchTcs;

                // Trigger batch delay if this is the last worker in the batch
                if (_currentBatchCount % _maxParallelWorkers == 0)
                {
                    _currentBatchCount = 0;
                    var nextBatchTcs = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously
                    );
                    _batchTcs = nextBatchTcs;

                    // Fire-and-forget batch delay
                    _ = TriggerBatchDelayAsync(nextBatchTcs, stoppingToken);
                }
            }

            // Wait for current batch to be released
            await tcsToAwait.Task.WaitAsync(stoppingToken);

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

    private async Task TriggerBatchDelayAsync(
        TaskCompletionSource nextBatchTcs,
        CancellationToken stoppingToken
    )
    {
        try
        {
            var delay = _downloadServiceSettingsMonitor.CurrentValue.BatchDelayMilliseconds;
            await Task.Delay(delay, stoppingToken);
            nextBatchTcs.SetResult(); // release next batch
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "💥 Delay of download jobs batch got cancelled - OperationCanceledException"
            );
        }
    }
}
