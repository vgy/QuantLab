namespace QuantLab.MarketData.Hub.Tests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Interface;

[TestFixture]
public class JobControllerTests
{
    private Mock<IMarketDataService> _marketMock = null!;
    private Mock<IIbkrDataService> _ibkrDataServiceMock = null!;
    private JobController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _marketMock = new();
        _ibkrDataServiceMock = new();
        _controller = new(_marketMock.Object, _ibkrDataServiceMock.Object);
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

    [Test]
    public async Task DownloadContractIds_Should_Invoke_DownloadContractIdsAsync_of_IbkrDataService()
    {
        const string expectedMessage = "Retrieved Contract Ids for 100 of 100 symbols";
        _ibkrDataServiceMock
            .Setup(m => m.DownloadContractIdsAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedMessage));

        var result = await _controller.DownloadContractIds();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        okResult!.Value.Should().BeEquivalentTo(new { message = expectedMessage });
        _ibkrDataServiceMock.Verify(
            m => m.DownloadContractIdsAsync(It.IsAny<string>()),
            Times.Once
        );
    }
}
