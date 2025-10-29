namespace QuantLab.MarketData.Hub.Services.Interface;

public interface IBackgroundJobQueue<T>
{
    Task<T> QueueAsync(Func<CancellationToken, Task<T>> workItem);
    Task<(Func<CancellationToken, Task<T>> WorkItem, TaskCompletionSource<T> Tcs)> DequeueAsync(
        CancellationToken cancellationToken
    );
}
