namespace QuantLab.MarketData.Hub.UnitTests.Services.Download.Ibkr;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Download;
using QuantLab.MarketData.Hub.Services.Interface.Storage;
using QuantLab.MarketData.Hub.UnitTests;

[TestFixture]
public class IbkrBarDownloadServiceTests
{
    private Mock<IDownloadQueue<ResponseData>> _downloadQueueMock = null!;
    private Mock<ICsvFileService> _csvFileServiceMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private Mock<IOptions<IbkrApiSettings>> _ibkrApiSettingsMock = null!;
    private Mock<ILogger<IbkrContractIdDownloadService>> _loggerMock = null!;
    private Mock<IbkrDownloadService> _ibkrDownloadServiceMock = null!;
    private IbkrBarDownloadService _ibkrBarDownloadService = null!;
    private IServiceProvider _serviceProviderMock = null!;
    private const string SymbolsAndContractIdsFileName = "sym_conIds.csv";
    private const string RetrySymbolsAndContractIdsFileName = "retry.csv";
    private const string HistoricalBarsRelativePathTemplate = "{0}\\{0}-{1}.csv";
    private const string HistoricalMarketDataEndPoint = "/marketdata/history";

    [SetUp]
    public void SetUp()
    {
        _downloadQueueMock = new();
        _csvFileServiceMock = new();
        _fileStorageSettingsMock = new();
        _ibkrApiSettingsMock = new();
        _loggerMock = new();
        _ibkrDownloadServiceMock = new(null!, null!);
        var fileStorageSettings = new FileStorageSettings
        {
            SymbolsFileName = SymbolsAndContractIdsFileName,
            RetrySymbolsAndContractIdsFileName = RetrySymbolsAndContractIdsFileName,
            HistoricalBarsRelativePathTemplate = HistoricalBarsRelativePathTemplate,
        };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);
        var ibkrApiSettings = new IbkrApiSettings
        {
            HistoricalMarketDataEndPoint = HistoricalMarketDataEndPoint,
        };
        _ibkrApiSettingsMock.Setup(x => x.Value).Returns(ibkrApiSettings);

        // Fake ServiceProvider & Scope
        var services = new ServiceCollection();
        services.AddScoped(_ => _ibkrDownloadServiceMock.Object);
        _serviceProviderMock = services.BuildServiceProvider();

        _ibkrBarDownloadService = new IbkrBarDownloadService(
            _downloadQueueMock.Object,
            _serviceProviderMock,
            _csvFileServiceMock.Object,
            _fileStorageSettingsMock.Object,
            _ibkrApiSettingsMock.Object,
            _loggerMock.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProviderMock is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_ValidInput_AllSymbolsDownloadedAndWritten()
    {
        // Arrange
        var symbols = new List<Symbol> { new("INFY", 101), new("TCS", 102) };

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var responseData1 = new ResponseData(
            "INFY",
            new() { { "data", CreateValidJsonElement() } }
        );
        var responseData2 = new ResponseData("TCS", new() { { "data", CreateValidJsonElement() } });

        _downloadQueueMock
            .SetupSequence(q =>
                q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>())
            )
            .ReturnsAsync(responseData1)
            .ReturnsAsync(responseData2);

        // Act
        var result = await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.OneMinute,
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().Contain("Retrieved Historical Bars");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(p => p.Contains("1m-INFY.csv")),
                    It.IsAny<List<Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(p => p.Contains("1m-TCS.csv")),
                    It.IsAny<List<Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName),
                    It.IsAny<List<Symbol>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLog(LogLevel.Information, "All 2 jobs queued");
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_EmptyResponseData_WritesRetryFile()
    {
        // Arrange
        var symbols = new List<Symbol> { new("RELIANCE", 999) };

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var emptyResponse = new ResponseData(
            "RELIANCE",
            new() { { "data", CreateEmptyJsonElement() } }
        );

        _downloadQueueMock
            .Setup(q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.FiveMinutes,
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().Contain("Retrieved Historical Bars of 5m for 0 of 1 symbols");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName),
                    It.Is<List<Symbol>>(l => l.Count == 1 && l[0].Name == "RELIANCE"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_ExceptionDuringParse_LogsErrorAndSkipsFileWrite()
    {
        // Arrange
        var symbols = new List<Symbol> { new("INFY", 123) };
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var badJson = new JsonElement(); // Invalid JSON (default)
        var badResponse = new ResponseData("INFY", new() { { "data", badJson } });

        _downloadQueueMock
            .Setup(q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()))
            .ReturnsAsync(badResponse);

        // Act
        var result = await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.OneDay,
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().Contain("Retrieved Historical Bars of 1d for 0 of 1 symbols");
        _loggerMock.VerifyLog(LogLevel.Error, "Parse error");
    }

    [TestCaseSource(nameof(BuildUrlTestCases))]
    public void BuildUrl_ValidInputs_ReturnsExpectedUrl(
        int conId,
        BarInterval interval,
        string expected
    )
    {
        var result = InvokeBuildUrl(conId, interval);
        result.Should().Be(expected);
    }

    [Test]
    public void BuildUrl_WithStartTime_IncludesQueryParam()
    {
        // Act
        var url = InvokeBuildUrl(100, BarInterval.OneMinute, "2024-01-01T00:00:00");

        // Assert
        url.Should().Contain("startTime=2024-01-01T00%3A00%3A00");
    }

    [Test]
    public void ParseResponseData_ValidResponse_ReturnsBars()
    {
        // Arrange
        var response = new ResponseData("INFY", new() { { "data", CreateValidJsonElement() } });

        // Act
        var bars = InvokeParseResponseData(BarInterval.OneMinute, response);

        // Assert
        bars.Should().HaveCount(2);
        bars[0].Symbol.Should().Be("INFY");
    }

    [Test]
    public void ParseResponseData_NoData_ReturnsEmptyList()
    {
        // Arrange
        var response = new ResponseData("TCS", []);

        // Act
        var result = InvokeParseResponseData(BarInterval.OneDay, response);

        // Assert
        result.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Error, "No data for TCS");
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_SomeTasksFail_ContinuesAndWritesPartialResults()
    {
        // Arrange
        var symbols = new List<Symbol> { new("INFY", 101), new("TCS", 102), new("HDFCBANK", 103) };

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var goodResponse = new ResponseData("INFY", new() { { "data", CreateValidJsonElement() } });
        var badResponse = new ResponseData("TCS", new() { { "data", CreateEmptyJsonElement() } });

        // Simulate: 1st succeeds, 2nd fails, 3rd throws
        _downloadQueueMock
            .SetupSequence(q =>
                q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>())
            )
            .ReturnsAsync(goodResponse)
            .ThrowsAsync(new HttpRequestException("Server error"))
            .ReturnsAsync(badResponse);

        // Act
        var result = await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.OneMinute,
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().Contain("Retrieved Historical Bars of 1m for 1 of 3 symbols");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(p => p.Contains("INFY.csv")),
                    It.IsAny<List<Bar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName),
                    It.Is<List<Symbol>>(l =>
                        l.Count == 2
                        && l.Any(s => s.Name == "TCS")
                        && l.Any(s => s.Name == "HDFCBANK")
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLog(
            LogLevel.Error,
            "One or more downloads failed. Continuing with available data."
        );
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_CancellationRequested_StopsProcessingGracefully()
    {
        // Arrange
        var symbols = new List<Symbol> { new("INFY", 123), new("TCS", 124) };

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Simulate external cancellation

        // QueueAsync should respect cancellation
        _downloadQueueMock
            .Setup(q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()))
            .Returns<Func<CancellationToken, Task<ResponseData>>>(fn =>
            {
                if (cts.Token.IsCancellationRequested)
                    throw new TaskCanceledException();
                return Task.FromResult(
                    new ResponseData("INFY", new() { { "data", CreateValidJsonElement() } })
                );
            });

        // Act
        await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.FiveMinutes,
            SymbolsAndContractIdsFileName
        );

        // Assert

        _loggerMock.VerifyLogMessage(LogLevel.Information, "✅ All 2 jobs queued", Times.Never());
        _loggerMock.VerifyLog(
            LogLevel.Error,
            "One or more downloads failed. Continuing with available data."
        );
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_TaskThrowsException_LogsAndWritesRetrySymbols()
    {
        // Arrange
        var symbols = new List<Symbol> { new("SBIN", 777) };
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        _downloadQueueMock
            .Setup(q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        // Act
        var result = await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.OneDay,
            SymbolsAndContractIdsFileName
        );

        // Assert
        result.Should().Contain("Retrieved Historical Bars of 1d for 0 of 1 symbols");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName),
                    It.Is<List<Symbol>>(list => list.Single().Name == "SBIN"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _loggerMock.VerifyLog(LogLevel.Error, "Unexpected failure");
    }

    [Test]
    public async Task DownloadHistoricalBarAsync_MultipleSymbols_AllQueuedInParallel()
    {
        // Arrange
        var symbols = Enumerable.Range(1, 5).Select(i => new Symbol($"SYM{i}", 100 + i)).ToList();

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<Func<string[], Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var response = new ResponseData("SYM", new() { { "data", CreateValidJsonElement() } });

        var queueCalls = new List<string>();
        _downloadQueueMock
            .Setup(q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()))
            .Callback<Func<CancellationToken, Task<ResponseData>>>(fn => queueCalls.Add("queued"))
            .ReturnsAsync(response);

        // Act
        await _ibkrBarDownloadService.DownloadHistoricalBarAsync(
            BarInterval.OneMinute,
            SymbolsAndContractIdsFileName
        );

        // Assert
        queueCalls.Should().HaveCount(5);
        _loggerMock.VerifyLog(LogLevel.Information, "All 5 jobs queued");
    }

    private static IEnumerable<TestCaseData> BuildUrlTestCases
    {
        get
        {
            yield return new TestCaseData(
                101,
                BarInterval.OneMinute,
                $"{HistoricalMarketDataEndPoint}?conid=101&exchange=NSE&period=1d&bar=1min"
            );
            yield return new TestCaseData(
                202,
                BarInterval.FiveMinutes,
                $"{HistoricalMarketDataEndPoint}?conid=202&exchange=NSE&period=2d&bar=5min"
            );
            yield return new TestCaseData(
                303,
                BarInterval.OneHour,
                $"{HistoricalMarketDataEndPoint}?conid=303&exchange=NSE&period=1m&bar=1h"
            );
            yield return new TestCaseData(
                404,
                BarInterval.OneDay,
                $"{HistoricalMarketDataEndPoint}?conid=404&exchange=NSE&period=1y&bar=1d"
            );
        }
    }

    private static JsonElement CreateValidJsonElement()
    {
        var json = """
            [
              {"t": 1, "o": 100.1, "h": 101.5, "l": 99.8, "c": 100.9, "v": 1000},
              {"t": 2, "o": 101.0, "h": 102.0, "l": 100.0, "c": 101.5, "v": 2000}
            ]
            """;
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static JsonElement CreateEmptyJsonElement()
    {
        using var doc = JsonDocument.Parse("[]");
        return doc.RootElement.Clone();
    }

    private string InvokeBuildUrl(int conId, BarInterval interval, string? startTime = null)
    {
        var method = typeof(IbkrBarDownloadService).GetMethod(
            "BuildUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        return (string)method.Invoke(_ibkrBarDownloadService, [conId, interval, startTime])!;
    }

    private List<Bar> InvokeParseResponseData(BarInterval interval, ResponseData response)
    {
        var method = typeof(IbkrBarDownloadService).GetMethod(
            "ParseResponseData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        return (List<Bar>)method.Invoke(_ibkrBarDownloadService, [interval, response])!;
    }
}
