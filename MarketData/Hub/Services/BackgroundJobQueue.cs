namespace QuantLab.MarketData.Hub.Services;

using System.Collections.Concurrent;

public interface IBackgroundJobQueue
{
	void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
	Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class BackgroundJobQueue() : IBackgroundJobQueue
{
	private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = [];
	private readonly SemaphoreSlim _signal = new(0);

	public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
	{
		ArgumentNullException.ThrowIfNull(workItem);
		_workItems.Enqueue(workItem);
		_signal.Release();
	}

	public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
	{
		await _signal.WaitAsync(cancellationToken);
		_workItems.TryDequeue(out var workItem);
		return workItem!;
	}
}