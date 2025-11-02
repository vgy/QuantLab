using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;

namespace QuantLab.MarketData.Hub.UnitTests.Controllers;

[TestFixture]
public class DataControllerTests
{
    private Mock<IMarketDataFetchService> _marketDataFetchServiceMock = null!;
    private DataController _dataController = null!;

    [SetUp]
    public void SetUp()
    {
        _marketDataFetchServiceMock = new();
        _dataController = new(_marketDataFetchServiceMock.Object);
    }

    [TestCase("ABC", "1d")]
    [TestCase("XYZ", "1h")]
    [TestCase("QSD", "5m")]
    public async Task GetData_ValidParameters_ReturnsOkWithMessage(
        string symbol,
        string barInterval
    )
    {
        // Arrange
        _ = BarInterval.TryParse(barInterval, out BarInterval? interval);
        IReadOnlyList<Bar> expectedResult =
        [
            new Bar
            {
                Symbol = symbol,
                Interval = interval!,
                Close = 28.15m,
            },
            new Bar
            {
                Symbol = symbol,
                Interval = interval!,
                Close = 29.15m,
            },
        ];
        _marketDataFetchServiceMock
            .Setup(s =>
                s.GetDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToString() == barInterval)
                )
            )
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _dataController.GetData(symbol, barInterval);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        (result as OkObjectResult)!
            .Value.Should()
            .BeEquivalentTo(
                new { message = expectedResult },
                options => options.WithStrictOrdering()
            );

        _marketDataFetchServiceMock.Verify(
            s =>
                s.GetDataAsync(
                    It.Is<string>(x => x == symbol),
                    It.Is<BarInterval>(b => b.ToString() == barInterval)
                ),
            Times.Once
        );
    }

    [Test]
    public async Task GetData_NullSymbol_ReturnsBadRequest()
    {
        // Arrange && Act
        var result = await _dataController.GetData(null!, "1d");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Symbol is required.");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("  ")]
    [TestCase("")]
    public async Task GetData_MissingSymbol_ReturnsBadRequest(string missingSymbolParam)
    {
        // Arrange && Act
        var result = await _dataController.GetData(missingSymbolParam, "1h");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Symbol is required.");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetData_NullBarInterval_ReturnsBadRequest()
    {
        // Arrange && Act
        var result = await _dataController.GetData("ABC", null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Bar Interval is required.");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }

    [TestCase("  ")]
    [TestCase("")]
    public async Task GetData_MissingBarInterval_ReturnsBadRequest(string missingBarIntervalParam)
    {
        // Arrange && Act
        var result = await _dataController.GetData("ABC", missingBarIntervalParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Bar Interval is required.");

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
    public async Task GetData_InvalidBarInterval_ReturnsBadRequest(string invalidBarIntervalParam)
    {
        // Arrange & Act
        var result = await _dataController.GetData("ABC", invalidBarIntervalParam);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult)!.Value.Should().Be("Bar Interval is invalid to fetch.");

        _marketDataFetchServiceMock.VerifyNoOtherCalls();
    }
}
