namespace QuantLab.MarketData.Hub.Tests.Services;

using FluentAssertions;
using QuantLab.MarketData.Hub.Services;
using NUnit.Framework;

[TestFixture]
public class BackgroundJobQueueTests
{
    private BackgroundJobQueue _queue = null!;

    [SetUp]
    public void SetUp() => _queue = new();

    [Test]
    public async Task QueueAndDequeue_Should_WorkCorrectly()
    {
        var executed = false;

        _queue.QueueBackgroundWorkItem(async _ =>
        {
            executed = true;
            await Task.CompletedTask;
        });

        var workItem = await _queue.DequeueAsync(default);
        await workItem(default);

        executed.Should().BeTrue();
    }

    [Test]
    public void QueueBackgroundWorkItem_Should_Throw_WhenNull()
    {
        var act = () => _queue.QueueBackgroundWorkItem(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task DequeueAsync_Should_WaitUntilItemIsQueued()
    {
        var cts = new CancellationTokenSource(500);

        var dequeueTask = Task.Run(() => _queue.DequeueAsync(cts.Token));

        await Task.Delay(100);
        _queue.QueueBackgroundWorkItem(_ => Task.CompletedTask);

        var result = await dequeueTask;
        result.Should().NotBeNull();
    }
}