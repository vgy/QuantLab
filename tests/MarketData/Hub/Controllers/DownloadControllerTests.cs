namespace QuantLab.MarketData.Hub.UnitTests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[TestFixture]
public class DownloadControllerTests
{
    private Mock<IIbkrContractIdDownloadService> _ibkrContractIdDownloadServiceMock = null!;
    private Mock<IIbkrBarDownloadService> _ibkrBarDownloadServiceMock = null!;
    private Mock<IIbkrTwsBarDownloadService> _ibkrTwsBarDownloadServiceMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private DownloadController _downloadController = null!;
    private const string SymbolsFileName = "sym.csv";
    private const string SymbolsAndContractIdsFileName = "sym_conIds.csv";
    private const string RetrySymbolsAndContractIdsFileName = "retry.csv";

    [SetUp]
    public void SetUp()
    {
        _ibkrContractIdDownloadServiceMock = new();
        _ibkrBarDownloadServiceMock = new();
        _ibkrTwsBarDownloadServiceMock = new();
        _fileStorageSettingsMock = new();
        var fileStorageSettings = new FileStorageSettings
        {
            SymbolsFileName = SymbolsFileName,
            SymbolsAndContractIdsFileName = SymbolsAndContractIdsFileName,
            RetrySymbolsAndContractIdsFileName = RetrySymbolsAndContractIdsFileName,
        };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);
        _downloadController = new(
            _ibkrContractIdDownloadServiceMock.Object,
            _ibkrBarDownloadServiceMock.Object,
            _ibkrTwsBarDownloadServiceMock.Object,
            _fileStorageSettingsMock.Object
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
            m => m.DownloadContractIdsAsync(It.Is<string>(x => x == SymbolsFileName)),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadContractIds_WhenCalled_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "✅ Contract IDs downloaded successfully";
        _ibkrContractIdDownloadServiceMock
            .Setup(s => s.DownloadContractIdsAsync(It.Is<string>(x => x == SymbolsFileName)))
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadContractIds();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { message = expectedMessage });

        _ibkrContractIdDownloadServiceMock.Verify(
            s => s.DownloadContractIdsAsync(It.Is<string>(x => x == SymbolsFileName)),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBars_ValidInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "Bars retrieved successfully";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadHistoricalBars("5min");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrBarDownloadServiceMock.Verify(
            s =>
                s.DownloadHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToShortString() == "5min"),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBars_MissingInterval_ReturnsBadRequest()
    {
        // Arrange
        string? invalidParam = "";

        // Act
        var result = await _downloadController.DownloadHistoricalBars(invalidParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is required");

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
    public async Task DownloadHistoricalBars_InvalidInterval_ReturnsBadRequest(string invalidParam)
    {
        // Arrange & Act
        var result = await _downloadController.DownloadHistoricalBars(invalidParam); // whitespace invalid

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is invalid");

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task DownloadHistoricalBarsForMissedSymbols_ValidInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "✅ Retry bars retrieved successfully";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadHistoricalBarsForMissedSymbols("5min");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrBarDownloadServiceMock.Verify(
            s =>
                s.DownloadHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToShortString() == "5min"),
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBars_PrivateHelper_CallsServiceCorrectly()
    {
        // Arrange
        const string expectedMessage = "✅ Bars downloaded internally";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await InvokePrivateDownloadHistoricalBars("1h", SymbolsAndContractIdsFileName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });
    }

    private async Task<IActionResult> InvokePrivateDownloadHistoricalBars(
        string interval,
        string file
    )
    {
        var method = typeof(DownloadController).GetMethod(
            "DownloadHistoricalBars",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var task = (Task<IActionResult>)method!.Invoke(_downloadController, [interval, file])!;
        return await task;
    }

    [Test]
    public async Task DownloadTwsHistoricalBars_ValidInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "Bars retrieved successfully";
        _ibkrTwsBarDownloadServiceMock
            .Setup(s =>
                s.DownloadTwsHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadTwsHistoricalBars("5min");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrTwsBarDownloadServiceMock.Verify(
            s =>
                s.DownloadTwsHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToShortString() == "5min"),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadTwsHistoricalBars_MissingInterval_ReturnsBadRequest()
    {
        // Arrange
        string? invalidParam = "";

        // Act
        var result = await _downloadController.DownloadTwsHistoricalBars(invalidParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is required");

        _ibkrTwsBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("???")]
    [TestCase("20h")]
    [TestCase("100m")]
    [TestCase("1")]
    [TestCase("m")]
    [TestCase("h")]
    [TestCase("d")]
    [TestCase("adssff")]
    public async Task DownloadTwsHistoricalBars_InvalidInterval_ReturnsBadRequest(
        string invalidParam
    )
    {
        // Arrange & Act
        var result = await _downloadController.DownloadTwsHistoricalBars(invalidParam); // whitespace invalid

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is invalid");

        _ibkrTwsBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task DownloadTwsHistoricalBarsForMissedSymbols_ValidInterval_ReturnsOkWithMessage()
    {
        // Arrange
        const string expectedMessage = "✅ Retry bars retrieved successfully";
        _ibkrTwsBarDownloadServiceMock
            .Setup(s =>
                s.DownloadTwsHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await _downloadController.DownloadTwsHistoricalBarsForMissedSymbols("5min");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });

        _ibkrTwsBarDownloadServiceMock.Verify(
            s =>
                s.DownloadTwsHistoricalBarAsync(
                    It.Is<BarInterval>(b => b.ToShortString() == "5min"),
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadTwsHistoricalBars_PrivateHelper_CallsServiceCorrectly()
    {
        // Arrange
        const string expectedMessage = "✅ Bars downloaded internally";
        _ibkrTwsBarDownloadServiceMock
            .Setup(s =>
                s.DownloadTwsHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);

        // Act
        var result = await InvokePrivateDownloadTwsHistoricalBars(
            "1h",
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(new { message = expectedMessage });
    }

    private async Task<IActionResult> InvokePrivateDownloadTwsHistoricalBars(
        string interval,
        string file
    )
    {
        var method = typeof(DownloadController).GetMethod(
            "DownloadTwsHistoricalBars",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        var task = (Task<IActionResult>)method!.Invoke(_downloadController, [interval, file])!;
        return await task;
    }
}
