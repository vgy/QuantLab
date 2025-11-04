using Grpc.Core;
using Grpc.Core.Testing;

namespace QuantLab.MarketData.Hub.UnitTests.Grpc;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Grpc;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;
using QuantLab.Protos.MarketData;
using Bar = Hub.Models.Domain.Bar;

[TestFixture]
public class MarketDataGrpcServiceTests
{
    private Mock<IMarketDataFetchService> _marketDataFetchServiceMock = null!;
    private MarketDataGrpcService _marketDataGrpcService = null!;

    [SetUp]
    public void SetUp()
    {
        _marketDataFetchServiceMock = new();
        _marketDataGrpcService = new(_marketDataFetchServiceMock.Object);
    }

    [TestCase("ABC", "5m")]
    [TestCase("DEF", "1d")]
    [TestCase("XYZ", "1h")]
    public async Task GetMarketData_WhenCalledWithValidBarInterval_ReturnsExpectedMessage(
        string symbol,
        string interval
    )
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        List<Bar> expectedBars =
        [
            new Bar
            {
                Symbol = symbol,
                Interval = barInterval,
                Timestamp = 123456789,
                Open = 12.45m,
                High = 29.46m,
                Low = 1.28m,
                Close = 19.79m,
                Volume = 123456,
            },
            new Bar
            {
                Symbol = symbol,
                Interval = barInterval,
                Timestamp = 987654321,
                Open = 14.45m,
                High = 31.46m,
                Low = 4.28m,
                Close = 21.79m,
                Volume = 456789,
            },
        ];
        var expectedMessage =
            $"Fetched {expectedBars.Count} records for {interval} interval of {symbol}";
        var barsGrpc = expectedBars.Select(b => b.ToProto());
        _marketDataFetchServiceMock
            .Setup(s =>
                s.GetMarketDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToShortString() == interval)
                )
            )
            .ReturnsAsync(expectedBars);
        var request = new MarketDataRequest { Symbol = symbol, Interval = interval };

        // Act
        var result = await _marketDataGrpcService.GetMarketData(request, CreateFakeContext());

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MarketDataResponse>();
        result.Message.Should().Be(expectedMessage);
        result.Bars.Should().BeEquivalentTo(barsGrpc);

        _marketDataFetchServiceMock.Verify(
            s =>
                s.GetMarketDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToShortString() == interval)
                ),
            Times.Once
        );
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task GetMarketData_WhenCalledithWhiteSpaceAsSymbol_ReturnsSymbolIsRequired(
        string invalidParam
    )
    {
        // Arrange
        var request = new MarketDataRequest { Interval = "5m" };
        var context = CreateFakeContext();

        // Act
        var result = await _marketDataGrpcService.GetMarketData(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MarketDataResponse>();
        result.Message.Should().Be("Symbol is required");
        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task GetMarketData_WhenCalledithWhiteSpaceAsBarInterval_ReturnsIntervalIsRequired(
        string invalidParam
    )
    {
        // Arrange
        var request = new MarketDataRequest { Symbol = "ABC", Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _marketDataGrpcService.GetMarketData(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MarketDataResponse>();
        result.Message.Should().Be("Interval is required");
        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("???")]
    [TestCase("20h")]
    [TestCase("100m")]
    [TestCase("1")]
    [TestCase("m")]
    [TestCase("h")]
    [TestCase("d")]
    [TestCase("adssff")]
    public async Task GetMarketData_WhenCalledithInvalidBarInterval_ReturnsInvalidRequest(
        string invalidParam
    )
    {
        // Arrange
        var request = new MarketDataRequest { Symbol = "ABC", Interval = invalidParam };
        var context = CreateFakeContext();

        // Act
        var result = await _marketDataGrpcService.GetMarketData(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MarketDataResponse>();
        result.Message.Should().Be("Interval is invalid to fetch");
        _marketDataFetchServiceMock.VerifyNoOtherCalls();
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
