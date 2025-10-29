namespace QuantLab.MarketData.Hub.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Interface;

public sealed class BackgroundWorkerService<T> : BackgroundService
{
    private readonly IBackgroundJobQueue<T> _jobQueue;
    private readonly ILogger<BackgroundWorkerService<T>> _logger;
    private readonly int _maxParallelWorkers;

    public BackgroundWorkerService(
        IBackgroundJobQueue<T> jobQueue,
        ILogger<BackgroundWorkerService<T>> logger,
        IOptions<BackgroundWorkerOptions> options
    )
    {
        _jobQueue = jobQueue;
        _logger = logger;
        _maxParallelWorkers = options.Value.MaxParallelWorkers;
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
            var (workItem, tcs) = await _jobQueue.DequeueAsync(stoppingToken);

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
