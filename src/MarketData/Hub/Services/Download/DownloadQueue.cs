namespace QuantLab.MarketData.Hub.Services.Download;

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Interface.Download;

/*
Backpressure means system pushes back on producers when it’s overloaded.

Without it, producers can enqueue thousands of jobs instantly — consuming memory,
exhausting the queue, and potentially crashing the app.

With backpressure, if the queue already has too many pending jobs (say 100),
new enqueue attempts wait (or are rejected) until the queue drains.
*/

public sealed class DownloadQueue<T> : IDownloadQueue<T>, IDisposable
{
    private readonly ConcurrentQueue<(
        Func<CancellationToken, Task<T>> QueueItem,
        TaskCompletionSource<T> Tcs
    )> _queueItems = [];
    private readonly SemaphoreSlim _signal = new(0);
    private readonly SemaphoreSlim _queueSlots; // Backpressure. Controls how many can be queued at once.

    public DownloadQueue(IOptions<MaxDownloadSettings> maxDownloadSettings)
    {
        int maxQueueSize = maxDownloadSettings.Value.MaxQueueSize;
        _queueSlots = new SemaphoreSlim(maxQueueSize, maxQueueSize);
    }

    public async Task<T> QueueAsync(Func<CancellationToken, Task<T>> queueItem)
    {
        ArgumentNullException.ThrowIfNull(queueItem);

        // Wait until there's space in the queue
        await _queueSlots.WaitAsync();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queueItems.Enqueue((queueItem, tcs));

        // Notify workers
        _signal.Release();

        return await tcs.Task;
    }

    public async Task<(
        Func<CancellationToken, Task<T>> QueueItem,
        TaskCompletionSource<T> Tcs
    )> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queueItems.TryDequeue(out var item);

        // Free one queue slot now that one task is dequeued
        _queueSlots.Release();

        return item;
    }

    public void Dispose()
    {
        _signal.Dispose();
        _queueSlots.Dispose();
    }
}
