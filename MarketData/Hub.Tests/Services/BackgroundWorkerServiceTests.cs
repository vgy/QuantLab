namespace QuantLab.MarketData.Hub.Tests.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using QuantLab.MarketData.Hub.Services;
using NUnit.Framework;

[TestFixture]
public class BackgroundWorkerServiceTests
{
    private Mock<IBackgroundJobQueue> _queueMock = null!;
    private Mock<ILogger<BackgroundWorkerService>> _loggerMock = null!;
    private BackgroundWorkerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _queueMock = new();
        _loggerMock = new();
        _service = new(_queueMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ExecuteAsync_Should_Run_QueuedWorkItem()
    {
        var executed = false;
        _queueMock.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(async _ =>
            {
                executed = true;
                await Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource(200);
        var task = _service.StartAsync(cts.Token);

        await Task.Delay(100);
        cts.Cancel();

        await task;

        executed.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_Should_Handle_Exception_Gracefully()
    {
        _queueMock.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(async _ => throw new InvalidOperationException("Boom"));

        using var cts = new CancellationTokenSource(150);
        await _service.StartAsync(cts.Token);

        await Task.Delay(50);
        cts.Cancel();

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(e => e is InvalidOperationException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}