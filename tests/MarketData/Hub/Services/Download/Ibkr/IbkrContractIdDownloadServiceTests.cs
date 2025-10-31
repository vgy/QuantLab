namespace QuantLab.MarketData.Hub.UnitTests.Services.Download.Ibkr;

using System.Text.Json;
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
public class IIbkrContractIdDownloadServiceTests
{
    private Mock<IDownloadQueue<ResponseData>> _downloadQueueMock = null!;
    private Mock<ICsvFileService> _csvFileServiceMock = null!;
    private Mock<IbkrDownloadService> _ibkrDownloadServiceMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private Mock<ILogger<IbkrContractIdDownloadService>> _loggerMock = null!;

    private IbkrContractIdDownloadService _ibkrContractIdDownloadService = null!;

    private IServiceProvider _serviceProviderMock = null!;
    private const string SymbolsFileName = "sym.csv";
    private const string SymbolsAndContractIdsFileName = "sym_conIds.csv";

    [SetUp]
    public void SetUp()
    {
        _downloadQueueMock = new Mock<IDownloadQueue<ResponseData>>();
        _csvFileServiceMock = new Mock<ICsvFileService>();
        _ibkrDownloadServiceMock = new Mock<IbkrDownloadService>(null!, null!);
        _fileStorageSettingsMock = new();
        _loggerMock = new Mock<ILogger<IbkrContractIdDownloadService>>();
        var fileStorageSettings = new FileStorageSettings
        {
            SymbolsFileName = SymbolsFileName,
            SymbolsAndContractIdsFileName = SymbolsAndContractIdsFileName,
        };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);

        // Fake ServiceProvider & Scope
        var services = new ServiceCollection();
        services.AddScoped(_ => _ibkrDownloadServiceMock.Object);
        _serviceProviderMock = services.BuildServiceProvider();

        _ibkrContractIdDownloadService = new IbkrContractIdDownloadService(
            _downloadQueueMock.Object,
            _serviceProviderMock,
            _csvFileServiceMock.Object,
            _fileStorageSettingsMock.Object,
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
    public async Task DownloadContractIdsAsync_FileWithSymbols_QueuesJobsAndWritesFile()
    {
        // Arrange
        var symbols = new[] { "NIFTY", "BANKNIFTY" };
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsFileName),
                    It.IsAny<Func<string[], string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var responses = symbols
            .Select(symbol => new ResponseData
            {
                Symbol = symbol,
                Data = new Dictionary<string, object>
                {
                    [symbol] = JsonSerializer.Deserialize<JsonElement>("""[{"conid":12345}]"""),
                },
            })
            .ToList();

        _downloadQueueMock
            .SetupSequence(q =>
                q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>())
            )
            .Returns(Task.FromResult(responses[0]))
            .Returns(Task.FromResult(responses[1]));

        _csvFileServiceMock
            .Setup(f =>
                f.WriteAsync(
                    It.Is<string>(s => s == SymbolsAndContractIdsFileName),
                    It.IsAny<IEnumerable<Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _ibkrContractIdDownloadService.DownloadContractIdsAsync(SymbolsFileName);

        // Assert
        result.Should().Be("Retrieved Contract Ids for 2 of 2 symbols");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.Is<List<Symbol>>(l => l.Count == 2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _downloadQueueMock.Verify(
            q => q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>()),
            Times.Exactly(2)
        );

        _loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce());
    }

    [Test]
    public async Task DownloadContractIdsAsync_EmptyDataInResponse_ParsesNullAndWritesPartialResults()
    {
        // Arrange
        var symbols = new[] { "AAPL", "TSLA" };
        _csvFileServiceMock
            .Setup(f =>
                f.ReadAsync(
                    It.Is<string>(x => x == SymbolsFileName),
                    It.IsAny<Func<string[], string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(symbols);

        var validResponse = new ResponseData
        {
            Symbol = "AAPL",
            Data = new Dictionary<string, object>
            {
                ["AAPL"] = JsonSerializer.Deserialize<JsonElement>("""[{"conid":123}]"""),
            },
        };
        var invalidResponse = new ResponseData
        {
            Symbol = "TSLA",
            Data = new Dictionary<string, object>(),
        };

        _downloadQueueMock
            .SetupSequence(q =>
                q.QueueAsync(It.IsAny<Func<CancellationToken, Task<ResponseData>>>())
            )
            .Returns(Task.FromResult(validResponse))
            .Returns(Task.FromResult(invalidResponse));

        _csvFileServiceMock
            .Setup(f =>
                f.WriteAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.IsAny<IEnumerable<Symbol>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _ibkrContractIdDownloadService.DownloadContractIdsAsync(SymbolsFileName);

        // Assert
        result.Should().Be("Retrieved Contract Ids for 1 of 2 symbols");

        _csvFileServiceMock.Verify(
            f =>
                f.WriteAsync(
                    It.Is<string>(x => x == SymbolsAndContractIdsFileName),
                    It.Is<List<Symbol>>(l => l.Count == 1 && l[0].Name == "AAPL"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public void ParseResponseData_ValidResponse_ReturnsSymbol()
    {
        // Arrange
        var jsonElement = JsonSerializer.Deserialize<JsonElement>("""[{"conid":999}]""");
        var response = new ResponseData
        {
            Symbol = "IBM",
            Data = new Dictionary<string, object> { ["IBM"] = jsonElement },
        };

        // Act
        var result = InvokePrivateParseResponseData(response);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Name.Should().Be("IBM");
        result.Value.CurrentFuturesContractId.Should().Be(999);
    }

    [Test]
    public void ParseResponseData_EmptyData_ReturnsNullAndLogsError()
    {
        // Arrange
        var response = new ResponseData
        {
            Symbol = "GOOG",
            Data = new Dictionary<string, object>(),
        };

        // Act
        var result = InvokePrivateParseResponseData(response);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public void ParseResponseData_MalformedJson_LogsErrorAndReturnsNull()
    {
        // Arrange
        var badJsonElement = JsonSerializer.Deserialize<JsonElement>("""{"unexpected":true}""");
        var response = new ResponseData
        {
            Symbol = "MSFT",
            Data = new Dictionary<string, object> { ["MSFT"] = badJsonElement },
        };

        // Act
        var result = InvokePrivateParseResponseData(response);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    private Symbol? InvokePrivateParseResponseData(ResponseData responseData)
    {
        var method = typeof(IbkrContractIdDownloadService).GetMethod(
            "ParseResponseData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        return (Symbol?)
            method.Invoke(_ibkrContractIdDownloadService, new object[] { responseData });
    }
}
