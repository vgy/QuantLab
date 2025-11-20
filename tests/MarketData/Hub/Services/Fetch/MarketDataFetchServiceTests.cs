namespace QuantLab.MarketData.Hub.UnitTests.Services.Fetch;

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Fetch;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

[TestFixture]
public class MarketDataFetchServiceTests
{
    private Mock<ICsvFileService> _csvFileServiceMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private Mock<ILogger<MarketDataFetchService>> _loggerMock = null!;

    private MarketDataFetchService _marketDataFetchService = null!;
    private const string HistoricalBarsRelativePathTemplate = "{0}\\{0}-{1}.csv";

    [SetUp]
    public void SetUp()
    {
        _csvFileServiceMock = new();
        _fileStorageSettingsMock = new();
        _loggerMock = new();
        var fileStorageSettings = new FileStorageSettings
        {
            HistoricalBarsRelativePathTemplate = HistoricalBarsRelativePathTemplate,
        };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);

        _marketDataFetchService = new MarketDataFetchService(
            _csvFileServiceMock.Object,
            _fileStorageSettingsMock.Object,
            _loggerMock.Object
        );
    }

    [TestCase("ABC", "1d")]
    [TestCase("XYZ", "1h")]
    [TestCase("QSD", "5min")]
    [TestCase("ZSX", "15min")]
    public async Task GetDataAsync_ValidParameters_ReturnsBars(string symbol, string interval)
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        var filePath = string.Format(HistoricalBarsRelativePathTemplate, interval, symbol);
        List<Bar> expectedResult =
        [
            new Bar
            {
                Symbol = symbol,
                Interval = barInterval,
                Close = 28.15m,
            },
            new Bar
            {
                Symbol = symbol,
                Interval = barInterval,
                Close = 29.15m,
            },
        ];
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == filePath),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _marketDataFetchService.GetMarketDataAsync(symbol, barInterval);

        // Assert
        result.Should().BeEquivalentTo(expectedResult, options => options.WithStrictOrdering());

        _csvFileServiceMock.Verify(
            f =>
                f.ReadAsync(
                    It.Is<string>(x => x == filePath),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLogMessage(
            LogLevel.Information,
            $"Fetched {expectedResult.Count} records for {interval} of {symbol} from {filePath}",
            Times.AtLeastOnce()
        );
    }

    [Test]
    public void GetDataAsync_NullSymbol_ThrowsArgumentException()
    {
        // Act && Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _marketDataFetchService.GetMarketDataAsync(null!, BarInterval.OneDay)
        );
        _csvFileServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("  ")]
    [TestCase("")]
    public void GetDataAsync_WhiteSpaceAsSymbol_ThrowsArgumentException(string missingSymbolParam)
    {
        // Act && Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _marketDataFetchService.GetMarketDataAsync(
                missingSymbolParam,
                BarInterval.OneHour
            )
        );
        _csvFileServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("ABC", "1d")]
    [TestCase("XYZ", "1h")]
    [TestCase("QSD", "5min")]
    [TestCase("ZSX", "15min")]
    public async Task GetDataAsync_WhenCsvFileServiceReturnsEmptyList_ReturnsEmptyList(
        string symbol,
        string interval
    )
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        var filePath = string.Format(HistoricalBarsRelativePathTemplate, interval, symbol);
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        // Act
        var result = await _marketDataFetchService.GetMarketDataAsync(symbol, barInterval);

        // Assert
        result.Should().BeEmpty();

        _csvFileServiceMock.Verify(
            f =>
                f.ReadAsync(
                    It.Is<string>(x => x == filePath),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLogMessage(
            LogLevel.Information,
            $"Fetched 0 records for {interval} of {symbol} from {filePath}",
            Times.AtLeastOnce()
        );
    }

    [TestCase("ABC", "1d")]
    [TestCase("XYZ", "1h")]
    [TestCase("QSD", "5min")]
    [TestCase("ZSX", "15min")]
    public async Task GetDataAsync_WhenCsvFileServiceThrowsException_ReturnsEmptyList(
        string symbol,
        string interval
    )
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        var filePath = string.Format(HistoricalBarsRelativePathTemplate, interval, symbol);
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Throws<Exception>();

        // Act
        var result = await _marketDataFetchService.GetMarketDataAsync(symbol, barInterval);

        // Assert
        result.Should().BeEmpty();

        _csvFileServiceMock.Verify(
            f =>
                f.ReadAsync(
                    It.Is<string>(x => x == filePath),
                    It.IsAny<Func<string[], Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLogMessage(
            LogLevel.Error,
            $"Data Fetch Error for {interval} of {symbol} from {filePath}",
            Times.AtLeastOnce()
        );
    }

    [TestCase("ABC", "1d", "2025-11-20 15:45:30", 28.5, 31.68, 21.99, 31.29, 879846)]
    [TestCase("XYW", "1h", "2025-01-20 01:00:00", 38.5, 41.49, 31.49, 41.20, 456789)]
    [TestCase("QSD", "5min", "2025-10-18 10:45:30", 48.5, 51.68, 41.90, 51.99, 123465)]
    [TestCase("ZSX", "15min", "2025-11-20 20:45:00", 48.5, 51.68, 41.90, 51.99, 123465)]
    public void ParseResponseData_ValidValues_ReturnsBar(
        string symbol,
        string interval,
        string timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        int volume
    )
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        var bar = new Bar
        {
            Symbol = symbol,
            Interval = barInterval,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
        };
        string[] values =
        [
            symbol,
            interval,
            timestamp,
            open.ToString(),
            high.ToString(),
            low.ToString(),
            close.ToString(),
            volume.ToString(),
        ];
        var filePath = string.Format(HistoricalBarsRelativePathTemplate, interval, symbol);

        // Act
        var result = InvokePrivateParseBar(values, filePath);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(bar);
        _loggerMock.VerifyLog(It.IsAny<LogLevel>(), Times.Never());
    }

    [Test]
    public void ParseBar_EmptyData_ThrowsExceptionAndLogsError()
    {
        // Arrange
        string[] values = [];
        var fileName = string.Format(HistoricalBarsRelativePathTemplate, "1d", "ABC");

        // Act && Assert
        Action act = () => InvokePrivateParseBar(values, fileName);
        act.Should()
            .Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage($"Parsing error in {string.Join(',', values)} of {fileName}");
    }

    [TestCase("ABC", "ab", "123456789", "28.5", "31.68", "21.99", "31.29", "879846")]
    [TestCase("XYW", "1h", "987654321", "abc", "41.49", "31.49", "41.20", "456789")]
    [TestCase("QSD", "5m", "123465798", "48.5", "defg", "41.90", "51.99", "123465")]
    [TestCase("QSD", "5m", "123465798", "48.5", "51.68", "hijkl", "51.99", "123465")]
    [TestCase("QSD", "5m", "123465798", "48.5", "51.68", "41.90", "mnop", "123465")]
    [TestCase("QSD", "5m", "123465798", "48.5", "51.68", "41.90", "51.99", "qrs")]
    public void ParseBar_MalformedValues_ThrowsExceptionAndLogsError(
        string symbol,
        string interval,
        string timestamp,
        string open,
        string high,
        string low,
        string close,
        string volume
    )
    {
        // Arrange
        string[] values = [symbol, interval, timestamp, open, high, low, close, volume];
        var fileName = string.Format(HistoricalBarsRelativePathTemplate, interval, symbol);

        // Act && Assert
        Action act = () => InvokePrivateParseBar(values, fileName);
        act.Should()
            .Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage($"Parsing error in {string.Join(',', values)} of {fileName}");
    }

    private Bar? InvokePrivateParseBar(string[] values, string fileName)
    {
        var method = typeof(MarketDataFetchService).GetMethod(
            "ParseBar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!;
        return (Bar?)method.Invoke(_marketDataFetchService, [values, fileName]);
    }
}
