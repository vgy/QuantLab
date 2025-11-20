using Grpc.Core;
using Grpc.Core.Testing;

namespace QuantLab.MarketData.Hub.UnitTests.Grpc;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Grpc;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.Protos.MarketData;

[TestFixture]
public class DownloadGrpcServiceTests
{
    private Mock<IIbkrContractIdDownloadService> _ibkrContractIdDownloadServiceMock = null!;
    private Mock<IIbkrBarDownloadService> _ibkrBarDownloadServiceMock = null!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private DownloadGrpcService _downloadGrpcService = null!;
    private const string SymbolsFileName = "sym.csv";
    private const string SymbolsAndContractIdsFileName = "sym_conIds.csv";
    private const string RetrySymbolsAndContractIdsFileName = "retry.csv";

    [SetUp]
    public void SetUp()
    {
        _ibkrContractIdDownloadServiceMock = new();
        _ibkrBarDownloadServiceMock = new();
        _fileStorageSettingsMock = new();
        var fileStorageSettings = new FileStorageSettings
        {
            SymbolsFileName = SymbolsFileName,
            SymbolsAndContractIdsFileName = SymbolsAndContractIdsFileName,
            RetrySymbolsAndContractIdsFileName = RetrySymbolsAndContractIdsFileName,
        };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);
        _downloadGrpcService = new(
            _ibkrContractIdDownloadServiceMock.Object,
            _ibkrBarDownloadServiceMock.Object,
            _fileStorageSettingsMock.Object
        );
    }

    [Test]
    public async Task DownloadContractIds_Should_Invoke_DownloadContractIdsAsync_of_IbkrContractIdDownloadService()
    {
        // Arrange
        const string expectedMessage = "Retrieved Contract Ids for 100 of 100 symbols";
        _ibkrContractIdDownloadServiceMock
            .Setup(m => m.DownloadContractIdsAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedMessage));

        // Act
        var result = await _downloadGrpcService.DownloadContractIds(new(), CreateFakeContext());

        // Assert
        _ibkrContractIdDownloadServiceMock.Verify(
            m => m.DownloadContractIdsAsync(It.Is<string>(x => x == SymbolsFileName)),
            Times.Once
        );
    }

    [Test]
    public async Task DownloadContractIds_WhenCalled_ReturnsExpectedMessage()
    {
        // Arrange
        const string expectedMessage = "Retrieved Contract Ids for 100 of 100 symbols";
        _ibkrContractIdDownloadServiceMock
            .Setup(s => s.DownloadContractIdsAsync(It.Is<string>(x => x == SymbolsFileName)))
            .ReturnsAsync(expectedMessage);
        var context = CreateFakeContext();

        // Act
        var result = await _downloadGrpcService.DownloadContractIds(new(), CreateFakeContext());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = expectedMessage });
    }

    [Test]
    public async Task DownloadHistoricalBars_WhenCalledWithValidBarInterval_ReturnsExpectedMessage()
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
        var request = new HistoricalBarsRequest { Interval = "5min" };

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(
            request,
            CreateFakeContext()
        );

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = expectedMessage });

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
    public async Task DownloadHistoricalBars_WhenCalledithMissingBarInterval_ReturnsBaRIntervalIsRequired()
    {
        // Arrange
        var request = new HistoricalBarsRequest();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(
            request,
            CreateFakeContext()
        );

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is required" });

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task DownloadHistoricalBars_WhenCalledithWhiteSpaceAsBarInterval_ReturnsBaRIntervalIsRequired(
        string invalidParam
    )
    {
        // Arrange
        var request = new HistoricalBarsRequest { Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is required" });

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
    public async Task DownloadHistoricalBars_WhenCalledithInvalidBarInterval_ReturnsInvalidRequest(
        string invalidParam
    )
    {
        // Arrange
        var request = new HistoricalBarsRequest { Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is invalid" });

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task DownloadHistoricalBarsForMissedSymbols_WhenCalledWithValidBarInterval_ReturnsExpectedMessage()
    {
        // Arrange
        const string expectedMessage = "Bars retrieved successfully";
        _ibkrBarDownloadServiceMock
            .Setup(s =>
                s.DownloadHistoricalBarAsync(
                    It.IsAny<BarInterval>(),
                    It.Is<string>(x => x == RetrySymbolsAndContractIdsFileName)
                )
            )
            .ReturnsAsync(expectedMessage);
        var request = new HistoricalBarsRequest { Interval = "5min" };

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBarsForMissedSymbols(
            request,
            CreateFakeContext()
        );

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = expectedMessage });

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
    public async Task DownloadHistoricalBarsForMissedSymbols_WhenCalledithMissingBarInterval_ReturnsBaRIntervalIsRequired()
    {
        // Arrange
        var request = new HistoricalBarsRequest();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(
            request,
            CreateFakeContext()
        );

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is required" });

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task DownloadHistoricalBarsForMissedSymbols_WhenCalledithWhiteSpaceAsBarInterval_ReturnsBaRIntervalIsRequired(
        string invalidParam
    )
    {
        // Arrange
        var request = new HistoricalBarsRequest { Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is required" });

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
    public async Task DownloadHistoricalBarsForMissedSymbols_WhenCalledithInvalidBarInterval_ReturnsInvalidRequest(
        string invalidParam
    )
    {
        // Arrange
        var request = new HistoricalBarsRequest { Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _downloadGrpcService.DownloadHistoricalBars(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<StatusReply>();
        result.Should().BeEquivalentTo(new StatusReply { Message = "Interval is invalid" });

        _ibkrBarDownloadServiceMock.VerifyNoOtherCalls();
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
            writeOptionsGetter: () => new WriteOptions(),
            writeOptionsSetter: (writeOptions) => { }
        );
}
