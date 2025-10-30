namespace QuantLab.MarketData.Hub.UnitTests.Services.Download;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Download;
using QuantLab.MarketData.Hub.Services.Interface.Download;

[TestFixture]
public sealed class DownloadBackgroundServiceTests
{
    private Mock<IDownloadQueue<int>> _mockDownloadQueue = null!;
    private Mock<ILogger<DownloadBackgroundService<int>>> _mockLogger = null!;
    private Mock<IOptionsMonitor<DownloadServiceSettings>> _mockSettingsMonitor = null!;
    private DownloadServiceSettings _settings;
    private DownloadBackgroundService<int> _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockDownloadQueue = new Mock<IDownloadQueue<int>>();
        _mockLogger = new Mock<ILogger<DownloadBackgroundService<int>>>();
        _mockSettingsMonitor = new Mock<IOptionsMonitor<DownloadServiceSettings>>();

        _settings = new DownloadServiceSettings
        {
            MaxParallelWorkers = 2,
            BatchDelayMilliseconds = 50,
        };

        _mockSettingsMonitor.Setup(m => m.CurrentValue).Returns(() => _settings);

        _service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (_service is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public async Task ExecuteAsync_WhenStarted_ShouldStartMultipleWorkers()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        int dequeueCalls = 0;

        _mockDownloadQueue
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

        var service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );

        // Act
        await service.StartAsync(cts.Token); // starts ExecuteAsync
        await Task.Delay(100); // allow workers to spin
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

        _mockDownloadQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var workerTask = Task.Run(() =>
            typeof(DownloadBackgroundService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, [1, cts.Token])
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

        _mockDownloadQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var workerTask = Task.Run(() =>
            typeof(DownloadBackgroundService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, [1, cts.Token])
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

        _mockDownloadQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((workItem, tcs));

        var service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        var workerTask = Task.Run(() =>
            typeof(DownloadBackgroundService<int>)
                .GetMethod(
                    "WorkerLoopAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )!
                .Invoke(service, [1, cts.Token])
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

        _mockDownloadQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((_ => throw new InvalidOperationException("Boom"), tcs));

        // Access private WorkerLoopAsync via reflection
        var method = typeof(DownloadBackgroundService<int>).GetMethod(
            "WorkerLoopAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        Assert.That(method, Is.Not.Null);

        var service = new DownloadBackgroundService<int>(
            _mockDownloadQueue.Object,
            _mockLogger.Object,
            _mockSettingsMonitor.Object
        );

        // Act
        var task = (Task)method!.Invoke(service, [1, cts.Token])!;
        await Task.Yield(); // allow the worker loop to start

        cts.Cancel(); // stop the loop

        // Assert
        await Task.Delay(50); // let exception propagate
        Assert.That(tcs.Task.IsFaulted, Is.True);
        Assert.That(tcs.Task.Exception!.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task WorkerLoopAsync_AllJobsExecuted_JobsCompleteSuccessfully()
    {
        // Arrange
        int totalJobs = 4;
        int executed = 0;
        var jobQueue = new Queue<(Func<CancellationToken, Task<int>>, TaskCompletionSource<int>)>();
        for (int i = 0; i < totalJobs; i++)
        {
            int id = i;
            jobQueue.Enqueue(
                (
                    async ct =>
                    {
                        Interlocked.Increment(ref executed);
                        await Task.Delay(10, ct);
                        return id;
                    },
                    new TaskCompletionSource<int>()
                )
            );
        }

        SetupAsyncQueue(jobQueue);

        var cts = new CancellationTokenSource();
        var workers = CreateWorkerTasks(_settings.MaxParallelWorkers, cts.Token);

        // Act
        await Task.Delay(500); // let workers finish
        cts.Cancel();
        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping background workers
        }

        // Assert
        Assert.That(executed, Is.EqualTo(totalJobs));
    }

    [Test]
    public async Task WorkerLoopAsync_JobThrowsException_TcsReceivesException()
    {
        // Arrange
        var jobQueue = new Queue<(Func<CancellationToken, Task<int>>, TaskCompletionSource<int>)>();
        var tcs = new TaskCompletionSource<int>();

        jobQueue.Enqueue(
            (
                async ct =>
                {
                    await Task.Delay(10, ct);
                    throw new InvalidOperationException("Test exception");
                },
                tcs
            )
        );

        SetupAsyncQueue(jobQueue);

        var cts = new CancellationTokenSource();
        var workers = CreateWorkerTasks(_settings.MaxParallelWorkers, cts.Token);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await tcs.Task);
        Assert.That(ex.Message, Is.EqualTo("Test exception"));

        // Cancel workers safely
        cts.Cancel();
        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping background workers
        }
    }

    [Test]
    public async Task WorkerLoopAsync_BatchDelay_IsRespectedBetweenBatches()
    {
        // Arrange
        int totalJobs = 4; // two batches
        int batchDelayMs = 100;
        _settings.BatchDelayMilliseconds = batchDelayMs;

        int executedJobs = 0;
        var jobStartTimes = new ConcurrentBag<DateTime>();

        var jobQueue = new Queue<(Func<CancellationToken, Task<int>>, TaskCompletionSource<int>)>();
        for (int i = 0; i < totalJobs; i++)
        {
            int jobId = i;
            jobQueue.Enqueue(
                (
                    async ct =>
                    {
                        jobStartTimes.Add(DateTime.UtcNow);
                        Interlocked.Increment(ref executedJobs);
                        await Task.Delay(10, ct); // respect token
                        return jobId;
                    },
                    new TaskCompletionSource<int>(
                        TaskCreationOptions.RunContinuationsAsynchronously
                    )
                )
            );
        }

        SetupAsyncQueue(jobQueue);

        var cts = new CancellationTokenSource();
        var workers = CreateWorkerTasks(_settings.MaxParallelWorkers, cts.Token);

        // Act
        // Wait long enough for both batches to start and finish
        await Task.Delay(batchDelayMs * 3);
        cts.Cancel();

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping background workers
        }

        // Assert
        Assert.That(executedJobs, Is.EqualTo(totalJobs), "Not all jobs executed.");

        var startTimesOrdered = jobStartTimes.OrderBy(t => t).ToArray();
        Assert.That(startTimesOrdered.Length, Is.GreaterThanOrEqualTo(totalJobs));

        var firstBatchEnd = startTimesOrdered[_settings.MaxParallelWorkers - 1];
        var secondBatchStart = startTimesOrdered[_settings.MaxParallelWorkers];

        var elapsed = secondBatchStart - firstBatchEnd;

        // Allow 5ms jitter tolerance
        Assert.That(
            elapsed,
            Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(batchDelayMs - 5)),
            $"Expected at least {batchDelayMs}ms between batches, but got {elapsed.TotalMilliseconds}ms."
        );
    }

    [Test]
    public async Task WorkerLoopAsync_DynamicBatchDelayUpdate_IsRespected()
    {
        // Arrange
        int totalJobs = 8; // two batches
        int initialDelay = 50;
        int updatedDelay = 200;
        _settings.BatchDelayMilliseconds = initialDelay;

        int executedJobs = 0;
        int jobCount = 0;
        DateTime? secondBatchStart = null;

        var jobQueue = new Queue<(Func<CancellationToken, Task<int>>, TaskCompletionSource<int>)>();
        var tcsList = new List<TaskCompletionSource<int>>();

        for (int i = 0; i < totalJobs; i++)
        {
            var tcs = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            tcsList.Add(tcs);

            int jobId = i;
            jobQueue.Enqueue(
                (
                    async ct =>
                    {
                        Interlocked.Increment(ref executedJobs);

                        int count = Interlocked.Increment(ref jobCount);
                        if (count == _settings.MaxParallelWorkers + 1)
                            secondBatchStart = DateTime.UtcNow;

                        await Task.Delay(10, ct);
                        return jobId;
                    },
                    tcs
                )
            );
        }

        SetupAsyncQueue(jobQueue);

        var cts = new CancellationTokenSource();
        var workers = CreateWorkerTasks(_settings.MaxParallelWorkers, cts.Token);

        // Act
        await Task.Delay(100); // allow first batch to start
        _settings.BatchDelayMilliseconds = updatedDelay; // update live
        await Task.WhenAny(Task.WhenAll(tcsList.Select(t => t.Task)), Task.Delay(5000)); // wait up to 5s

        cts.Cancel();

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // expected, ignore
        }

        // Assert
        Assert.That(executedJobs, Is.EqualTo(totalJobs), "Not all jobs executed");
        Assert.That(secondBatchStart, Is.Not.Null, "Second batch never started");
    }

    private void SetupAsyncQueue(
        Queue<(Func<CancellationToken, Task<int>>, TaskCompletionSource<int>)> queue
    )
    {
        _mockDownloadQueue
            .Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns(
                async (CancellationToken ct) =>
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        (
                            Func<CancellationToken, Task<int>> workItem,
                            TaskCompletionSource<int> tcs
                        ) item;

                        lock (queue)
                        {
                            if (queue.Count > 0)
                            {
                                item = queue.Dequeue();
                                Assert.That(
                                    item.workItem,
                                    Is.Not.Null,
                                    "Work item delegate is null"
                                );
                                Assert.That(item.tcs, Is.Not.Null, "TaskCompletionSource is null");
                                return item;
                            }
                        }

                        try
                        {
                            // small polling delay, swallow cancellation here safely
                            await Task.Delay(10, ct);
                        }
                        catch (TaskCanceledException)
                        {
                            // Exit gracefully when cancelled
                            ct.ThrowIfCancellationRequested();
                        }
                    }
                }
            );
    }

    private Task[] CreateWorkerTasks(int workerCount, CancellationToken token)
    {
        return
        [
            .. Enumerable
                .Range(0, workerCount)
                .Select(i => _service.InvokeWorkerLoopForTestAsync(i + 1, token)!),
        ];
    }
}

// --- Helper to access private WorkerLoopAsync ---

static class ServiceExtensions
{
    public static Task? InvokeWorkerLoopForTestAsync(
        this DownloadBackgroundService<int> service,
        int workerId,
        CancellationToken ct
    )
    {
        var method = typeof(DownloadBackgroundService<int>).GetMethod(
            "WorkerLoopAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        return (Task)method!.Invoke(service, [workerId, ct])!;
    }
}
