namespace QuantLab.MarketData.Hub.UnitTests.Models.Domain;

using System;
using System.Linq;
using FluentAssertions;
using Google.Protobuf.Collections;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Models.DTO.Grpc;
using BarGrpc = QuantLab.Protos.MarketData.Bar;

[TestFixture]
public class BarExtensionsTests
{
    [TestCase("ABC", "1d", 123465789, 28.5, 3168, 0.2199, 31.29, 879846)]
    [TestCase("XYW", "1h", 987654321, 38.5, 4149, 0.3149, 41.24, 456789)]
    [TestCase("QSD", "5m", 123465798, 48.5, 5168, 0.4191, 51.99, 123465)]
    public void ToProto_ValidBar_ShouldMapAllFieldsCorrectly(
        string symbol,
        string interval,
        long timestamp,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        int volume
    )
    {
        // Arrange
        _ = BarInterval.TryParse(interval, out BarInterval? barInterval);
        var bar = new Bar
        {
            Symbol = symbol,
            Interval = barInterval!,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
        };

        // Act
        var pb = bar.ToProto();

        // Assert
        pb.Symbol.Should().Be(symbol);
        pb.Interval.Should().Be(interval);
        pb.Timestamp.Should().Be(timestamp);
        pb.Volume.Should().Be(volume);
        ((int)pb.Open.Units).Should().Be((int)(open * 10));
        pb.Open.Scale.Should().Be(1);
        ((int)pb.High.Units).Should().Be((int)high);
        pb.High.Scale.Should().Be(0);
        ((int)pb.Low.Units).Should().Be((int)(low * 10000));
        pb.Low.Scale.Should().Be(4);
        ((int)pb.Close.Units).Should().Be((int)(close * 100));
        pb.Close.Scale.Should().Be(2);
    }

    [Test]
    public void AddRangeFast_MultipleBars_ShouldAddAllWithoutAllocations()
    {
        // Arrange
        var bars = new[]
        {
            new Bar("BTCUSD", BarInterval.OneDay, 111, 1.23m, 1.5m, 1.1m, 1.4m, 1000),
            new Bar("ETHUSD", BarInterval.OneDay, 222, 2.23m, 2.5m, 2.1m, 2.4m, 2000),
            new Bar("DOGEUSD", BarInterval.OneDay, 333, 0.123m, 0.15m, 0.11m, 0.14m, 3000),
        };

        var target = new RepeatedField<BarGrpc>();

        // Act
        target.AddRangeFast(bars);

        // Assert
        target.Count.Should().Be(bars.Length);
        target.Select(b => b.Symbol).Should().Contain(["BTCUSD", "ETHUSD", "DOGEUSD"]);
        target.All(b => b.Open is not null).Should().BeTrue();
    }

    [Test]
    public void AddRangeFast_EmptySource_ShouldNotThrowOrAdd()
    {
        // Arrange
        var target = new RepeatedField<BarGrpc>();
        ReadOnlySpan<Bar> source = [];

        // Act
        target.AddRangeFast(source);

        // Assert
        target.Count.Should().Be(0);
    }

    [Test]
    public void AddRangeFast_LargeSource_ShouldSetCapacityAndAddCorrectCount()
    {
        // Arrange
        var source = Enumerable
            .Range(1, 1000)
            .Select(i => new Bar(
                $"SYM{i}",
                BarInterval.FiveMinutes,
                i,
                10m + i,
                11m + i,
                9m + i,
                10.5m + i,
                i * 100
            ))
            .ToArray();

        var target = new RepeatedField<BarGrpc>();

        // Act
        target.AddRangeFast(source);

        // Assert
        target.Capacity.Should().Be(source.Length);
        target.Count.Should().Be(source.Length);
    }
}
