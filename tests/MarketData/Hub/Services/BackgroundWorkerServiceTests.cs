namespace QuantLab.MarketData.Hub.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Interface;

[TestFixture]
public sealed class BackgroundWorkerServiceTests
{
    private Mock<IBackgroundJobQueue<int>> _mockQueue = null!;
    private Mock<ILogger<BackgroundWorkerService<int>>> _mockLogger = null!;
    private IOptions<BackgroundWorkerOptions> _options = null!;

    [SetUp]
    public void Setup()
    {
        _mockQueue = new Mock<IBackgroundJobQueue<int>>();
        _mockLogger = new Mock<ILogger<BackgroundWorkerService<int>>>();
        _options = Options.Create(
            new BackgroundWorkerOptions { MaxParallelWorkers = 3, MaxQueueSize = 100 }
        );
    }

    [Test]
    public void Constructor_WhenCalled_SetsMaxParallelWorkersFromOptions()
    {
        // Act
        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_WhenStarted_ShouldStartMultipleWorkers()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        int dequeueCalls = 0;

        _mockQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns(
                async (CancellationToken token) =>
                {
                    await Task.Delay(10, token);
                    Interlocked.Increment(ref dequeueCalls);
                    return (
                        new Func<CancellationToken, Task<int>>(_ => Task.FromResult(1)),
                        new TaskCompletionSource<int>()
                    );
                }
            );

        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );

        // Act
        await service.StartAsync(cts.Token); // starts ExecuteAsync
        await Task.Delay(200); // allow workers to spin
        await service.StopAsync(cts.Token); // stop gracefully

        // Assert
        Assert.That(
            dequeueCalls,
            Is.GreaterThanOrEqualTo(3),
            "Expected multiple workers (>=3) to call DequeueAsync."
        );
    }

    [Test]
    public async Task WorkerLoopAsync_WhenWorkItemSucceeds_ShouldSetResultAndLog()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        var workItem = new Func<CancellationToken, Task<int>>(_ => Task.FromResult(42));

        _mockQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var workerTask = Task.Run(() =>
            typeof(BackgroundWorkerService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, new object[] { 1, cts.Token })
        );

        await Task.Delay(50); // Let worker process
        cts.Cancel(); // Stop loop

        await Task.WhenAny(workerTask, Task.Delay(500));

        // Assert
        Assert.That(
            tcs.Task.IsCompletedSuccessfully,
            Is.True,
            "Worker should complete TaskCompletionSource"
        );
        Assert.That(await tcs.Task, Is.EqualTo(42));

        _mockLogger.VerifyLog(LogLevel.Information, Times.AtLeast(1));
    }

    [Test]
    public async Task WorkerLoopAsync_WhenWorkItemThrows_ShouldSetExceptionAndLogError()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        var workItem = new Func<CancellationToken, Task<int>>(_ =>
            throw new InvalidOperationException("boom")
        );

        _mockQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var workerTask = Task.Run(() =>
            typeof(BackgroundWorkerService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, new object[] { 1, cts.Token })
        );

        await Task.Delay(50);
        cts.Cancel();

        await Task.WhenAny(workerTask, Task.Delay(500));

        // Assert
        Assert.That(
            tcs.Task.IsFaulted,
            Is.True,
            "Expected worker to set exception on TaskCompletionSource"
        );
        Assert.That(tcs.Task.Exception!.InnerException, Is.TypeOf<InvalidOperationException>());

        _mockLogger.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public async Task WorkerLoopAsync_WhenCancellationRequested_ShouldExitGracefully()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        var workItem = new Func<CancellationToken, Task<int>>(_ => Task.FromResult(10));

        _mockQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        var workerTask = Task.Run(() =>
            typeof(BackgroundWorkerService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, new object[] { 1, cts.Token })
        );

        await Task.WhenAny(workerTask, Task.Delay(1000));

        // Assert
        Assert.That(
            workerTask.IsCompleted,
            Is.True,
            "Worker should exit when cancellation requested"
        );
    }

    // [Test]
    // public async Task ExecuteAsync_WhenMultipleWorkers_RunJobsInParallelWithoutHanging()
    // {
    //     // Arrange
    //     var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    //     var workerCount = _options.Value.MaxParallelWorkers;

    //     var jobTcsList = new List<TaskCompletionSource<int>>();
    //     for (int i = 0; i < workerCount; i++)
    //     {
    //         jobTcsList.Add(new TaskCompletionSource<int>());
    //     }

    //     int dequeueCallIndex = 0;
    //     _mockQueue
    //         .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
    //         .Returns<CancellationToken>(token =>
    //         {
    //             if (dequeueCallIndex < jobTcsList.Count)
    //             {
    //                 var tcs = jobTcsList[dequeueCallIndex];
    //                 dequeueCallIndex++;
    //                 return Task.FromResult(
    //                     (WorkItem: (Func<CancellationToken, Task<int>>)(_ => tcs.Task), Tcs: tcs)
    //                 );
    //             }

    //             // After all test jobs, return a canceled task to allow worker loops to exit
    //             return Task.FromCanceled<(
    //                 Func<CancellationToken, Task<int>> WorkItem,
    //                 TaskCompletionSource<int> Tcs
    //             )>(token);
    //         });

    //     var service = new BackgroundWorkerService<int>(
    //         _mockQueue.Object,
    //         _mockLogger.Object,
    //         _options
    //     );

    //     // Act
    //     var executeTask = service.StartAsync(cts.Token);

    //     await Task.Yield(); // let worker loops start

    //     // Complete all queued jobs
    //     foreach (var tcs in jobTcsList)
    //         tcs.SetResult(42);

    //     // Stop workers gracefully
    //     cts.Cancel();
    //     try
    //     {
    //         await executeTask;
    //     }
    //     catch (TaskCanceledException)
    //     {
    //         // expected, ignore
    //     }

    //     // Assert
    //     _mockQueue.Verify(
    //         q => q.DequeueAsync(It.IsAny<CancellationToken>()),
    //         Times.AtLeast(workerCount)
    //     );
    //     foreach (var tcs in jobTcsList)
    //     {
    //         Assert.That(tcs.Task.Result, Is.EqualTo(42));
    //     }
    // }

    [Test]
    public async Task WorkerLoopAsync_WhenJobThrows_SetsExceptionOnTaskCompletionSourceWithoutHanging()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var tcs = new TaskCompletionSource<int>();

        _mockQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (
                    (Func<CancellationToken, Task<int>>)(
                        _ => throw new InvalidOperationException("Boom")
                    ),
                    tcs
                )
            );

        // Access private WorkerLoopAsync via reflection
        var method = typeof(BackgroundWorkerService<int>).GetMethod(
            "WorkerLoopAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Assert.That(method, Is.Not.Null);

        var service = new BackgroundWorkerService<int>(
            _mockQueue.Object,
            _mockLogger.Object,
            _options
        );

        // Act
        var task = (Task)method!.Invoke(service, new object?[] { 1, cts.Token })!;
        await Task.Yield(); // allow the worker loop to start

        cts.Cancel(); // stop the loop

        // Assert
        await Task.Delay(50); // let exception propagate
        Assert.That(tcs.Task.IsFaulted, Is.True);
        Assert.That(tcs.Task.Exception!.InnerException, Is.TypeOf<InvalidOperationException>());
    }
}
