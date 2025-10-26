namespace QuantLab.MarketData.Hub.Tests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Services;
using NUnit.Framework;

[TestFixture]
public class JobControllerTests
{
    private Mock<IBackgroundJobQueue> _queueMock = null!;
    private Mock<IMarketDataService> _marketMock = null!;
    private Mock<ILogger<JobController>> _loggerMock = null!;
    private JobController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _queueMock = new();
        _marketMock = new Mock<IMarketDataService>();
        _loggerMock = new();
        _controller = new(_queueMock.Object, _marketMock.Object, _loggerMock.Object);
    }

    [Test]
    public void StartJob_Should_Return_Accepted_And_QueueJob()
    {
        var result = _controller.StartJob();
        result.Should().BeOfType<AcceptedResult>();
        _queueMock.Verify(q => q.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    [Test]
    public void StartMarketDataJob_Should_Invoke_Start()
    {
        var result = _controller.StartMarketDataJob();
        result.Should().BeOfType<AcceptedResult>();
        _marketMock.Verify(m => m.Start(), Times.Once);
    }

    [Test]
    public void StopMarketDataJob_Should_Invoke_Stop()
    {
        var result = _controller.StopMarketDataJob();
        result.Should().BeOfType<OkObjectResult>();
        _marketMock.Verify(m => m.Stop(), Times.Once);
    }

    [Test]
    public void GetMarketDataStatus_Should_Return_CurrentStatus()
    {
        var status = new MarketDataStatus { IsRunning = true };
        _marketMock.Setup(m => m.Status).Returns(status);

        var result = _controller.GetMarketDataStatus() as OkObjectResult;

        result.Should().NotBeNull();
        (result!.Value as MarketDataStatus)!.IsRunning.Should().BeTrue();
    }
}