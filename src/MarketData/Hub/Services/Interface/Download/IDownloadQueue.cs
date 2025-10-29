namespace QuantLab.MarketData.Hub.Services.Interface.Download;

public interface IDownloadQueue<T>
{
    Task<T> QueueAsync(Func<CancellationToken, Task<T>> queueItem);
    Task<(Func<CancellationToken, Task<T>> QueueItem, TaskCompletionSource<T> Tcs)> DequeueAsync(
        CancellationToken cancellationToken
    );
}
