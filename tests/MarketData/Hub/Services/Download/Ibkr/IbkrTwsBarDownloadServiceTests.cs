namespace QuantLab.MarketData.Hub.UnitTests.Services.Download.Ibkr;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Infrastructure.Time;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Storage;
using QuantLabBar = QuantLab.MarketData.Hub.Models.Domain.Bar;

[TestFixture]
public sealed class IbkrTwsBarDownloadServiceTests
{
    private Mock<ICsvFileService> _csvFileServiceMock = null!;
    private Mock<ITimeProvider> _timeProviderMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileOptionsMock = null!;
    private Mock<ILogger<IbkrTwsBarDownloadService>> _loggerMock = null!;
    private Mock<IbkrTwsService> _ibkrTwsServiceMock = null!;

    private IbkrTwsBarDownloadService _sut = null!;
    private IServiceProvider _serviceProviderMock = null!;

    private readonly string _retryFileName = "retry.csv";
    private readonly string _pathTemplate = "{0}/{1}.csv";

    [SetUp]
    public void SetUp()
    {
        _csvFileServiceMock = new();
        _timeProviderMock = new();
        _fileOptionsMock = new();
        _loggerMock = new();
        _ibkrTwsServiceMock = new();

        // Fake ServiceProvider & Scope
        var services = new ServiceCollection();
        services.AddScoped(_ => _ibkrTwsServiceMock.Object);
        _serviceProviderMock = services.BuildServiceProvider();

        _fileOptionsMock
            .Setup(o => o.Value)
            .Returns(
                new FileStorageSettings
                {
                    RetrySymbolsAndContractIdsFileName = _retryFileName,
                    HistoricalBarsRelativePathTemplate = _pathTemplate,
                }
            );

        _sut = new(
            _serviceProviderMock,
            _csvFileServiceMock.Object,
            _timeProviderMock.Object,
            _fileOptionsMock.Object,
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

    [TestCase(BarInterval.FiveMinutes, "5 mins")]
    [TestCase(BarInterval.FifteenMinutes, "15 mins")]
    [TestCase(BarInterval.OneHour, "5 mins")]
    [TestCase(BarInterval.OneDay, "1 day")]
    [TestCase(BarInterval.OneWeek, "5 mins")]
    public void GetBarSizeSetting_ReturnsExpectedValue(
        BarInterval barInterval,
        string expectedValue
    )
    {
        // Act
        var result = InvokeGetBarSizeSetting(barInterval);

        // Assert
        Assert.That(result, Is.EqualTo(expectedValue));
    }

    private static string InvokeGetBarSizeSetting(BarInterval interval)
    {
        var method = typeof(IbkrTwsBarDownloadService).GetMethod(
            "GetBarSizeSetting",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!;

        return (string)method.Invoke(null, new object[] { interval })!;
    }

    [TestCase("ABC", "1d", "2025-11-20 15:45:30", 28.5, 31.68, 21.99, 31.29, 879846)]
    [TestCase("XYW", "1h", "2025-01-20 01:00:00", 38.5, 41.49, 31.49, 41.20, 456789)]
    [TestCase("QSD", "5min", "2025-10-18 10:45:30", 48.5, 51.68, 41.90, 51.99, 123465)]
    [TestCase("ZSX", "15min", "2025-11-20 20:45:00", 48.5, 51.68, 41.90, 51.99, 123465)]
    public void ParseBar_ValidValues_ReturnsBar(
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
        var bar = new QuantLabBar
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

        // Act
        var result = InvokeParseBar(values);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(bar.Symbol, Is.EqualTo(symbol));
            Assert.That(bar.Interval, Is.EqualTo(barInterval));
            Assert.That(bar.Timestamp, Is.EqualTo(timestamp));
            Assert.That(bar.Open, Is.EqualTo(open));
            Assert.That(bar.High, Is.EqualTo(high));
            Assert.That(bar.Low, Is.EqualTo(low));
            Assert.That(bar.Close, Is.EqualTo(close));
            Assert.That(bar.Volume, Is.EqualTo(volume));
        });
    }

    private static QuantLabBar InvokeParseBar(string[] values)
    {
        var method = typeof(IbkrTwsBarDownloadService).GetMethod(
            "ParseBar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!;

        return (QuantLabBar)method.Invoke(null, new object[] { values, "file.csv" })!;
    }

    [Test]
    public async Task AppendBars_ExistingBarsNotFromToday_AreKept()
    {
        // Arrange
        var fileName = "hist/NIFTY.csv";

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    fileName,
                    It.IsAny<Func<string[], QuantLabBar>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                [new() { Timestamp = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") }]
            );

        // Act
        await InvokeAppendBars(
            fileName,
            [new() { Timestamp = DateTime.Today.ToString("yyyy-MM-dd") }]
        );

        // Assert
        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<QuantLabBar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(p => p.Equals(fileName)),
                    It.Is<List<QuantLabBar>>(bars => bars.Count == 2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task AppendBars_ExistingBarsFromToday_AreReplaced()
    {
        // Arrange
        var fileName = "hist/NIFTY.csv";

        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    fileName,
                    It.IsAny<Func<string[], QuantLabBar>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([new() { Timestamp = DateTime.Today.ToString("yyyy-MM-dd"), Close = 1 }]);

        // Act
        await InvokeAppendBars(
            fileName,
            [new() { Timestamp = DateTime.Today.ToString("yyyy-MM-dd"), Close = 2 }]
        );

        // Assert

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<QuantLabBar>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(p => p.Equals(fileName)),
                    It.Is<List<QuantLabBar>>(bars => bars.Count == 1 && bars[0].Close == 2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private async Task InvokeAppendBars(string fileName, IList<QuantLabBar> bars)
    {
        var method = typeof(IbkrTwsBarDownloadService).GetMethod(
            "AppendBars",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;

        await (Task)method.Invoke(_sut, [fileName, bars])!;
    }
}

//     [Test]
//     public async Task DownloadTwsHistoricalBarAsync_AllSuccess_WritesBarsAndRetryFile()
//     {
//         // Arrange
//         var startTime = new DateTime(2025, 01, 01, 10, 0, 0);
//         var endTime = startTime.AddMinutes(5);

//         _timeProviderMock.SetupSequence(t => t.Now).Returns(startTime).Returns(endTime);

//         var futures = new List<FuturesContract> { new("NIFTY", 123), new("BANKNIFTY", 999) };

//         _csvFileServiceMock
//             .Setup(f =>
//                 f.ReadAsync(
//                     "input.csv",
//                     It.IsAny<Func<string[], FuturesContract>>(),
//                     It.IsAny<CancellationToken>()
//                 )
//             )
//             .ReturnsAsync(futures);

//         _ibkrTwsServiceMock
//             .Setup(s =>
//                 s.GetTwsHistoricalDataAsync(It.IsAny<Contract>(), "1 D", It.IsAny<string>())
//             )
//             .ReturnsAsync(
//                 (Contract c, string _, string __) =>
//                     new List<QuantLabBar>
//                     {
//                         new()
//                         {
//                             Symbol = c.Symbol,
//                             Interval = BarInterval.FiveMinutes,
//                             Timestamp = "2024-01-01",
//                             Open = 1,
//                             High = 1,
//                             Low = 1,
//                             Close = 1,
//                             Volume = 1,
//                         },
//                     }
//             );

//         _csvFileServiceMock
//             .Setup(f =>
//                 f.WriteAsync(
//                     It.IsAny<string>(),
//                     It.IsAny<IEnumerable<QuantLabBar>>(),
//                     It.IsAny<CancellationToken>()
//                 )
//             )
//             .Returns(Task.CompletedTask);

//         List<FuturesContract> retryCapture = new();

//         _csvFileServiceMock
//             .Setup(f =>
//                 f.WriteAsync(
//                     _retryFileName,
//                     It.IsAny<IEnumerable<FuturesContract>>(),
//                     It.IsAny<CancellationToken>()
//                 )
//             )
//             .Callback<string, IEnumerable<FuturesContract>>(
//                 (_, items) => retryCapture = items.ToList()
//             )
//             .Returns(Task.CompletedTask);

//         // Act
//         var msg = await _sut.DownloadTwsHistoricalBarAsync(BarInterval.FiveMinutes, "input.csv");

//         // Assert
//         Assert.Multiple(() =>
//         {
//             Assert.That(retryCapture, Is.Empty, "No retries expected.");
//             Assert.That(msg.Contains("Retrieved Historical Bars"), Is.True);
//         });
//     }

//     [Test]
//     public async Task DownloadTwsHistoricalBarAsync_HasFailures_LogsErrorAndWritesRetryFile()
//     {
//         // Arrange
//         var futures = new List<FuturesContract> { new("GOOD", 1), new("BAD", 2) };

//         _csvFileServiceMock
//             .Setup(f =>
//                 f.ReadAsync(
//                     "input.csv",
//                     It.IsAny<Func<string[], FuturesContract>>(),
//                     It.IsAny<CancellationToken>()
//                 )
//             )
//             .ReturnsAsync(futures);

//         _ibkrTwsServiceMock
//             .SetupSequence(s => s.GetTwsHistoricalDataAsync(It.IsAny<Contract>(), "1 D", "5 mins"))
//             .ReturnsAsync(
//                 new List<QuantLabBar> // GOOD works
//                 {
//                     new()
//                     {
//                         Symbol = "GOOD",
//                         Timestamp = "2024",
//                         Interval = BarInterval.FiveMinutes,
//                         Open = 1,
//                         High = 1,
//                         Low = 1,
//                         Close = 1,
//                         Volume = 1,
//                     },
//                 }
//             )
//             .ThrowsAsync(new Exception("Boom")); // BAD fails

//         List<FuturesContract> retryCapture = new();

//         _csvFileServiceMock
//             .Setup(f =>
//                 f.WriteAsync(
//                     _retryFileName,
//                     It.IsAny<IEnumerable<FuturesContract>>(),
//                     It.IsAny<CancellationToken>()
//                 )
//             )
//             .Callback<string, IEnumerable<FuturesContract>>(
//                 (_, items) => retryCapture = items.ToList()
//             )
//             .Returns(Task.CompletedTask);

//         _timeProviderMock.Setup(t => t.Now).Returns(DateTime.Now);

//         // Act
//         var msg = await _sut.DownloadTwsHistoricalBarAsync(BarInterval.FiveMinutes, "input.csv");

//         // Assert
//         Assert.That(retryCapture.First().Symbol, Is.EqualTo("BAD"));
//         Assert.That(msg.Contains("Retrieved Historical Bars"), Is.True);
//     }
// }

// class MockIbkrTwsService : IbkrTwsService, IMockIbkrTwsService
// {
//     public MockIbkrTwsService(ILogger<IbkrTwsService> logger)
//         : base(logger) { }

//     public async Task<List<QuantLabBar>> GetTwsHistoricalDataAsync(
//         Contract contract,
//         string durationStr,
//         string barSizeSetting
//     )
//     {
//         return [];
//     }
// }

// interface IMockIbkrTwsService
// {
//     Task<List<QuantLabBar>> GetTwsHistoricalDataAsync(
//         Contract contract,
//         string durationStr,
//         string barSizeSetting
//     );
// }
