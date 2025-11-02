namespace QuantLab.MarketData.Hub.UnitTests.Models.Domain;

using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Domain;

[TestFixture]
public class BarIntervalTests
{
    [Test]
    public void OneMinute_Always_ReturnsCorrectInstance()
    {
        // Arrange & Act
        var interval = BarInterval.OneMinute;

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(interval.Minutes, Is.EqualTo(1));
            Assert.That(interval.Symbol, Is.EqualTo("1m"));
        });
    }

    [Test]
    public void FiveMinutes_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.FiveMinutes;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(5));
            Assert.That(interval.Symbol, Is.EqualTo("5m"));
        });
    }

    [Test]
    public void FifteenMinutes_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.FifteenMinutes;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(15));
            Assert.That(interval.Symbol, Is.EqualTo("15m"));
        });
    }

    [Test]
    public void ThirtyMinutes_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.ThirtyMinutes;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(30));
            Assert.That(interval.Symbol, Is.EqualTo("30m"));
        });
    }

    [Test]
    public void OneHour_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.OneHour;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(60));
            Assert.That(interval.Symbol, Is.EqualTo("1h"));
        });
    }

    [Test]
    public void FourHours_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.FourHours;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(240));
            Assert.That(interval.Symbol, Is.EqualTo("4h"));
        });
    }

    [Test]
    public void SixHours_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.SixHours;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(360));
            Assert.That(interval.Symbol, Is.EqualTo("6h"));
        });
    }

    [Test]
    public void OneDay_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.OneDay;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(1440));
            Assert.That(interval.Symbol, Is.EqualTo("1d"));
        });
    }

    [Test]
    public void OneWeek_Always_ReturnsCorrectInstance()
    {
        var interval = BarInterval.OneWeek;

        Assert.Multiple(() =>
        {
            Assert.That(interval.Minutes, Is.EqualTo(10080));
            Assert.That(interval.Symbol, Is.EqualTo("1w"));
        });
    }

    [Test]
    public void All_ReturnsAllPredefinedIntervals_InCorrectOrder()
    {
        var expected = new[]
        {
            BarInterval.OneMinute,
            BarInterval.FiveMinutes,
            BarInterval.FifteenMinutes,
            BarInterval.ThirtyMinutes,
            BarInterval.OneHour,
            BarInterval.FourHours,
            BarInterval.SixHours,
            BarInterval.OneDay,
            BarInterval.OneWeek,
        };

        Assert.That(BarInterval.All, Is.EqualTo(expected));
    }

    [Test]
    public void All_IsReadOnly()
    {
        var all = BarInterval.All;
        Assert.That(all, Is.InstanceOf<IReadOnlyList<BarInterval>>());
    }

    [TestCase("1m", true)]
    [TestCase("5m", true)]
    [TestCase("15m", true)]
    [TestCase("30m", true)]
    [TestCase("1h", true)]
    [TestCase("4h", true)]
    [TestCase("6h", true)]
    [TestCase("1d", true)]
    [TestCase("1w", true)]
    [TestCase("1M", true)] // case-insensitive
    [TestCase("unknown", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void TryParse_Symbol_ReturnsExpectedResult(string? symbol, bool expectedResult)
    {
        // Act
        var result = BarInterval.TryParse(symbol, out var interval);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));

        if (expectedResult)
        {
            Assert.That(interval, Is.Not.Null);
            Assert.That(
                interval!.Symbol,
                Is.EqualTo(symbol).IgnoreCase,
                "Case-insensitive match expected"
            );
        }
        else
        {
            Assert.That(interval, Is.Null);
        }
    }

    [TestCase("1m", false)]
    [TestCase("5m", true)]
    [TestCase("15m", false)]
    [TestCase("30m", false)]
    [TestCase("1h", true)]
    [TestCase("4h", false)]
    [TestCase("6h", false)]
    [TestCase("1d", true)]
    [TestCase("1w", false)]
    [TestCase("1M", false)] // case-insensitive
    [TestCase("unknown", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void GetFetchInterval_Symbol_ReturnsExpectedResult(string? interval, bool expectedResult)
    {
        // Arrange
        _ = BarInterval.TryParse(interval, out BarInterval? barInterval);

        // Act
        var result = BarInterval.GetFetchInterval(interval!);

        // Assert
        result.IsValid.Should().Be(expectedResult);
        if (expectedResult)
            result.BarInterval.Should().Be(barInterval);
    }

    [Test]
    public void ToString_ReturnsSymbol()
    {
        var interval = BarInterval.OneHour;

        var str = interval.ToString();

        Assert.That(str, Is.EqualTo("1h"));
    }

    [Test]
    public void All_ToString_AllReturnSymbols()
    {
        foreach (var interval in BarInterval.All)
        {
            Assert.That(interval.ToString(), Is.EqualTo(interval.Symbol));
        }
    }

    [Test]
    public void All_StaticInstances_AreSameAsTryParseResult()
    {
        foreach (var interval in BarInterval.All)
        {
            var parsed = BarInterval.TryParse(interval.Symbol, out var found) ? found : null;
            Assert.That(parsed, Is.SameAs(interval));
        }
    }
}
