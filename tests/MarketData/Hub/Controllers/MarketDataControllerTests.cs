namespace QuantLab.MarketData.Hub.UnitTests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;

[TestFixture]
public class MarketDataControllerTests
{
    private Mock<IMarketDataFetchService> _marketDataFetchServiceMock = null!;
    private MarketDataController _marketDataController = null!;

    [SetUp]
    public void SetUp()
    {
        _marketDataFetchServiceMock = new();
        _marketDataController = new(_marketDataFetchServiceMock.Object);
    }

    [TestCase("ABC", "1d")]
    [TestCase("XYZ", "1h")]
    [TestCase("QSD", "5min")]
    [TestCase("ZSX", "15min")]
    public async Task GetMarketData_ValidParameters_ReturnsOkWithMessage(
        string symbol,
        string interval
    )
    {
        // Arrange
        _ = BarIntervalConverter.TryParse(interval, out BarInterval barInterval);
        IReadOnlyList<Bar> expectedBars =
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
        var expectedMessage =
            $"Fetched {expectedBars.Count} records for {interval} interval of {symbol}";
        _marketDataFetchServiceMock
            .Setup(s =>
                s.GetMarketDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToShortString() == interval)
                )
            )
            .ReturnsAsync(expectedBars);

        // Act
        var result = await _marketDataController.GetMarketData(symbol, interval);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(
                new { Message = expectedMessage, Bars = expectedBars },
                options => options.WithStrictOrdering()
            );

        _marketDataFetchServiceMock.Verify(
            s =>
                s.GetMarketDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToShortString() == interval)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task GetMarketData_NullSymbol_ReturnsBadRequest()
    {
        // Arrange && Act
        var result = await _marketDataController.GetMarketData(null!, "1d");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Symbol is required");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("  ")]
    [TestCase("")]
    public async Task GetMarketData_MissingSymbol_ReturnsBadRequest(string missingSymbolParam)
    {
        // Arrange && Act
        var result = await _marketDataController.GetMarketData(missingSymbolParam, "1h");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Symbol is required");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetMarketData_NullInterval_ReturnsBadRequest()
    {
        // Arrange && Act
        var result = await _marketDataController.GetMarketData("ABC", null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is required");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("  ")]
    [TestCase("")]
    public async Task GetMarketData_MissingInterval_ReturnsBadRequest(string missingIntervalParam)
    {
        // Arrange && Act
        var result = await _marketDataController.GetMarketData("ABC", missingIntervalParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is required");

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
    public async Task GetMarketData_InvalidInterval_ReturnsBadRequest(string invalidIntervalParam)
    {
        // Arrange & Act
        var result = await _marketDataController.GetMarketData("ABC", invalidIntervalParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Interval is invalid to fetch");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }
}
