namespace QuantLab.MarketData.Hub.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services;

[TestFixture]
public sealed class BackgroundJobQueueTests
{
    private static BackgroundJobQueue<T> CreateQueue<T>(int maxQueueSize = 100)
    {
        var options = Options.Create(new BackgroundWorkerOptions { MaxQueueSize = maxQueueSize });
        return new BackgroundJobQueue<T>(options);
    }

    [Test]
    public async Task QueueBackgroundWorkItem_WhenWorkItemIsQueued_ReturnsExpectedResult()
    {
        // Arrange
        var queue = CreateQueue<int>();
        var expected = 42;

        Func<CancellationToken, Task<int>> workItem = _ => Task.FromResult(expected);

        // Act
        var enqueueTask = queue.QueueAsync(workItem);
        var (dequeuedWork, tcs) = await queue.DequeueAsync(CancellationToken.None);

        var result = await dequeuedWork(CancellationToken.None);
        tcs.SetResult(result);

        var actual = await enqueueTask;

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void QueueBackgroundWorkItem_WhenWorkItemIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var queue = CreateQueue<int>();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => queue.QueueAsync(null!));
    }

    [Test]
    public async Task DequeueAsync_WhenNoItemsInitially_WaitsUntilItemIsQueued()
    {
        // Arrange
        var queue = CreateQueue<string>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5000));

        var dequeueTask = queue.DequeueAsync(cts.Token);

        // Act
        await Task.Delay(100); // Ensure DequeueAsync is waiting
        _ = queue.QueueAsync(_ => Task.FromResult("done"));

        var (workItem, tcs) = await dequeueTask;

        // Assert
        Assert.That(workItem, Is.Not.Null);
        Assert.That(tcs, Is.Not.Null);
    }

    [Test]
    public async Task QueueBackgroundWorkItem_WhenQueueIsFull_WaitsUntilSlotIsFreed()
    {
        // Arrange
        const int maxQueueSize = 1;
        var queue = CreateQueue<int>(maxQueueSize);

        // Fill queue with one item (reaching max capacity)
        _ = queue.QueueAsync(_ => Task.FromResult(10));

        var enqueueTask = queue.QueueAsync(_ => Task.FromResult(20));

        // Act
        // Dequeue one to free a slot
        var (workItem1, tcs1) = await queue.DequeueAsync(CancellationToken.None);
        await workItem1(CancellationToken.None);
        var (workItem2, tcs2) = await queue.DequeueAsync(CancellationToken.None);
        var result = await workItem2(CancellationToken.None);
        tcs2.SetResult(result);

        var finalResult = await enqueueTask;

        // Assert
        Assert.That(finalResult, Is.EqualTo(20));
    }

    [Test]
    public void DequeueAsync_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        var queue = CreateQueue<int>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () => await queue.DequeueAsync(cts.Token));
    }

    [Test]
    public void Dispose_WhenCalled_DisposesSemaphoresWithoutError()
    {
        // Arrange
        var queue = CreateQueue<int>();

        // Act & Assert
        Assert.DoesNotThrow(() => queue.Dispose());
    }

    [Test]
    public async Task DequeueAsync_WhenMultipleItemsQueued_DequeuesInFIFOOrder()
    {
        // Arrange
        var queue = CreateQueue<int>();

        var enqueueTask1 = queue.QueueAsync(_ => Task.FromResult(1));
        var enqueueTask2 = queue.QueueAsync(_ => Task.FromResult(2));

        // Act
        var first = await queue.DequeueAsync(CancellationToken.None);
        var second = await queue.DequeueAsync(CancellationToken.None);

        // Complete the dequeued tasks (simulate worker processing)
        first.Tcs.SetResult(await first.WorkItem(CancellationToken.None));
        second.Tcs.SetResult(await second.WorkItem(CancellationToken.None));

        var result1 = await enqueueTask1;
        var result2 = await enqueueTask2;

        // Assert
        Assert.That(result1, Is.EqualTo(1));
        Assert.That(result2, Is.EqualTo(2));
    }
}
