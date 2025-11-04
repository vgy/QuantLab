namespace QuantLab.MarketData.Hub.UnitTests.Models.Domain;

using FluentAssertions;
using QuantLab.MarketData.Hub.Models.Domain;

[TestFixture]
public class BarIntervalConverterTests
{
    [TestCase(BarInterval.FiveMinutes, "5m")]
    [TestCase(BarInterval.OneHour, "1h")]
    [TestCase(BarInterval.OneDay, "1d")]
    [TestCase(default(BarInterval), "5m")]
    public void ToShortString_WhenCalled_ReturnsCorrespondingShortString(
        BarInterval barInterval,
        string expectedShortString
    )
    {
        // Act && Assert
        barInterval.ToShortString().Should().Be(expectedShortString);
    }

    [TestCase("5m", true, BarInterval.FiveMinutes)]
    [TestCase("1h", true, BarInterval.OneHour)]
    [TestCase("1d", true, BarInterval.OneDay)]
    [TestCase("1m", false, BarInterval.FiveMinutes)]
    [TestCase("15m", false, BarInterval.FiveMinutes)]
    [TestCase("30m", false, BarInterval.FiveMinutes)]
    [TestCase("2h", false, BarInterval.FiveMinutes)]
    [TestCase("4h", false, BarInterval.FiveMinutes)]
    [TestCase("6h", false, BarInterval.FiveMinutes)]
    [TestCase("1w", false, BarInterval.FiveMinutes)]
    [TestCase("1m", false, BarInterval.FiveMinutes)]
    [TestCase("1y", false, BarInterval.FiveMinutes)]
    [TestCase("", false, BarInterval.FiveMinutes)]
    [TestCase("   ", false, BarInterval.FiveMinutes)]
    [TestCase("ABC", false, BarInterval.FiveMinutes)]
    [TestCase("s4dqs", false, BarInterval.FiveMinutes)]
    [TestCase("5456465", false, BarInterval.FiveMinutes)]
    [TestCase("!/*-+", false, BarInterval.FiveMinutes)]
    public void TryParse_WhenCalled_ReturnsCorrespondingBarInterval(
        string interval,
        bool expectedResult,
        BarInterval expectedBarInterval
    )
    {
        // Act
        var result = BarIntervalConverter.TryParse(interval.AsSpan(), out BarInterval barInterval);

        // Assert
        result.Should().Be(expectedResult);
        barInterval.Should().Be(expectedBarInterval);
    }
}
