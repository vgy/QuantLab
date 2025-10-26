using Grpc.Core;
using Grpc.Core.Testing;

namespace QuantLab.MarketData.Hub.Tests.Grpc;

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using QuantLab.MarketData.Hub.Grpc;
using QuantLab.MarketData.Hub.Services;
using NUnit.Framework;

[TestFixture]
public class JobGrpcServiceTests
{
    private Mock<IBackgroundJobQueue> _jobQueueMock = null!;
    private Mock<IMarketDataService> _marketDataServiceMock = null!;
    private Mock<ILogger<JobGrpcService>> _loggerMock = null!;
    private JobGrpcService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _jobQueueMock = new();
        _marketDataServiceMock = new Mock<IMarketDataService>();
        _loggerMock = new();
        _service = new(_jobQueueMock.Object, _marketDataServiceMock.Object, _loggerMock.Object);
    }

    private static ServerCallContext CreateFakeContext() =>
        TestServerCallContext.Create(
            method: "fakeMethod",
            host: "localhost",
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: [],
            cancellationToken: CancellationToken.None,
            peer: "127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            writeHeadersFunc: _ => Task.CompletedTask,
            writeOptionsGetter: () => new WriteOptions()
            , writeOptionsSetter: (writeOptions) => { });

    [Test]
    public async Task StartJob_Should_Queue_BackgroundWorkItem_And_Return_Message()
    {
        // Arrange
        var context = CreateFakeContext();
        _jobQueueMock.Setup(q => q.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()));

        // Act
        var reply = await _service.StartJob(new(), context);

        // Assert
        reply.Message.Should().Be("Background job started.");
        _jobQueueMock.Verify(q => q.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Test]
    public async Task StartMarketDataJob_Should_Invoke_Start()
    {
        // Arrange
        var context = CreateFakeContext();

        // Act
        var reply = await _service.StartMarketDataJob(new(), context);

        // Assert
        reply.Message.Should().Be("Market data job started.");
        _marketDataServiceMock.Verify(s => s.Start(), Times.Once);
    }

    [Test]
    public async Task StopMarketDataJob_Should_Invoke_Stop()
    {
        var context = CreateFakeContext();

        var reply = await _service.StopMarketDataJob(new(), context);

        reply.Message.Should().Be("Market data job stopped.");
        _marketDataServiceMock.Verify(s => s.Stop(), Times.Once);
    }

    [Test]
    public async Task GetMarketDataStatus_Should_Return_Mapped_Status()
    {
        // Arrange
        var context = CreateFakeContext();
        var fakeStatus = new MarketDataStatus
        {
            IsRunning = true,
            LastError = "Unauthorized",
            LastFetchTime = DateTime.UtcNow,
            LastResponseSnippet = "AAPL",
            FetchCount = 3
        };

        _marketDataServiceMock.Setup(s => s.Status).Returns(fakeStatus);

        // Act
        var result = await _service.GetMarketDataStatus(new(), context);

        // Assert
        result.IsRunning.Should().BeTrue();
        result.LastError.Should().Be("Unauthorized");
        result.LastResponseSnippet.Should().Be("AAPL");
        result.FetchCount.Should().Be(3);
        result.LastFetchTime.Should().NotBeEmpty();
    }

    [Test]
    public async Task StartJob_Should_Log_And_NotThrow()
    {
        var context = CreateFakeContext();
        var act = async () => await _service.StartJob(new(), context);

        await act.Should().NotThrowAsync();
        _loggerMock.VerifyNoOtherCalls();
    }
}
