namespace QuantLab.MarketData.Hub.UnitTests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[TestFixture]
public class DownloadControllerTests
{
    private Mock<IIbkrContractIdDownloadService> _ibkrContractIdDownloadServiceMock = null!;
    private Mock<IIbkrBarDownloadService> _ibkrBarDownloadServiceMock = null!;
    private DownloadController _downloadController = null!;

    [SetUp]
    public void SetUp()
    {
        _ibkrContractIdDownloadServiceMock = new();
        _ibkrBarDownloadServiceMock = new();
        _downloadController = new(
            _ibkrContractIdDownloadServiceMock.Object,
            _ibkrBarDownloadServiceMock.Object
        );
    }

    [Test]
    public async Task DownloadContractIds_Should_Invoke_DownloadContractIdsAsync_of_IbkrContractIdDownloadService()
    {
        const string expectedMessage = "Retrieved Contract Ids for 100 of 100 symbols";
        _ibkrContractIdDownloadServiceMock
            .Setup(m => m.DownloadContractIdsAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedMessage));

        var result = await _downloadController.DownloadContractIds();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        okResult!.Value.Should().BeEquivalentTo(new { message = expectedMessage });
        _ibkrContractIdDownloadServiceMock.Verify(
            m => m.DownloadContractIdsAsync(It.IsAny<string>()),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadContractIds_WhenCalled_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "✅ Contract IDs downloaded successfully";
        _ibkrContractIdDownloadServiceMock
            .Setup(s => s.DownloadContractIdsAsync("symbols.csv"))
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadContractIds();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { message = expectedMessage });

        _ibkrContractIdDownloadServiceMock.Verify(
            s => s.DownloadContractIdsAsync("symbols.csv"),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBar_ValidBarInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "Bars retrieved successfully";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(It.IsAny<BarInterval>(), "symbols_contractIds.csv")
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadHistoricalBar("1m");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrBarDownloadServiceMock.Verify(
            s =>
                s.DownloadHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToString() == "1m"),
                    "symbols_contractIds.csv"
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBar_MissingBarInterval_ReturnsBadRequest()
    {
        // Arrange
        string? invalidParam = "";

        // Act
        var result = await _downloadController.DownloadHistoricalBar(invalidParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!
            .Value.Should()
            .Be("Bar Interval parameter is required.");

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("???")]
    [TestCase("20h")]
    [TestCase("100m")]
    [TestCase("1")]
    [TestCase("m")]
    [TestCase("h")]
    [TestCase("d")]
    [TestCase("adssff")]
    public async Task DownloadHistoricalBar_InvalidBarInterval_ReturnsBadRequest(
        string invalidParam
    )
    {
        // Arrange & Act
        var result = await _downloadController.DownloadHistoricalBar(invalidParam); // whitespace invalid

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Bar Interval parameter is invalid.");

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task DownloadHistoricalBarForRetrySymbols_ValidBarInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "✅ Retry bars retrieved successfully";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    "retry_symbols_contractIds.csv"
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadHistoricalBarForRetrySymbols("5m");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrBarDownloadServiceMock.Verify(
            s =>
                s.DownloadHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToString() == "5m"),
                    "retry_symbols_contractIds.csv"
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBar_PrivateHelper_CallsServiceCorrectly()
    {
        // Arrange
        const string expectedMessage = "✅ Bars downloaded internally";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(It.IsAny<BarInterval>(), "symbols_contractIds.csv")
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await InvokePrivateDownloadHistoricalBar("1h", "symbols_contractIds.csv");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });
    }

    private async Task<IActionResult> InvokePrivateDownloadHistoricalBar(
        string interval,
        string file
    )
    {
        var method = typeof(DownloadController).GetMethod(
            "DownloadHistoricalBar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var task = (Task<IActionResult>)method!.Invoke(_downloadController, [interval, file])!;
        return await task;
    }
}
