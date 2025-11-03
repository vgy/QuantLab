namespace QuantLab.MarketData.Hub.UnitTests.Models.DTO.Grpc;

using FluentAssertions;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.DTO.Grpc;

[TestFixture]
public class DecimalValueTests
{
    [Test]
    public void Constructor_ValidInputs_ShouldInitializeFields()
    {
        // Arrange
        long expectedUnits = 12345;
        int expectedScale = 2;

        // Act
        var sut = new DecimalValue(expectedUnits, expectedScale);

        // Assert
        sut.Units.Should().Be(expectedUnits);
        sut.Scale.Should().Be(expectedScale);
    }

    [Test]
    public void Constructor_NegativeScale_ShouldAllowAndStoreValue()
    {
        // Arrange
        long units = 100;
        int scale = -2;

        // Act
        var sut = new DecimalValue(units, scale);

        // Assert
        sut.Scale.Should().Be(scale);
        sut.Units.Should().Be(units);
    }

    [Test]
    public void ToDecimal_ScaleZero_ReturnsUnitsAsDecimal()
    {
        // Arrange
        var sut = new DecimalValue(1234, 0);

        // Act
        var result = sut.ToDecimal();

        // Assert
        result.Should().Be(1234m);
    }

    [Test]
    public void ToDecimal_PositiveScale_ReturnsScaledDecimal()
    {
        // Arrange
        var sut = new DecimalValue(1234, 2);

        // Act
        var result = sut.ToDecimal();

        // Assert
        result.Should().Be(12.34m);
    }

    [Test]
    public void ToDecimal_HighScale_ShouldComputeCorrectly()
    {
        // Arrange
        var sut = new DecimalValue(1234, 5);

        // Act
        var result = sut.ToDecimal();

        // Assert
        result.Should().Be(0.01234m);
    }

    [Test]
    public void ToDecimal_ZeroUnits_ReturnsZeroRegardlessOfScale()
    {
        // Arrange
        var sut = new DecimalValue(0, 10);

        // Act
        var result = sut.ToDecimal();

        // Assert
        result.Should().Be(0m);
    }

    [TestCase(5654.52126, 565452126, 5)]
    [TestCase(24.56, 2456, 2)]
    [TestCase(24.5, 245, 1)]
    [TestCase(24.50, 245, 1)]
    [TestCase(2450, 2450, 0)]
    [TestCase(245000, 245000, 0)]
    public void FromDecimal_PositiveValue_ReturnsCorrectUnitsAndScale(
        decimal d,
        long units,
        int scale
    )
    {
        // Act
        var result = DecimalValue.FromDecimal(d);

        // Assert
        result.Units.Should().Be(units);
        result.Scale.Should().Be(scale);
    }

    [Test]
    public void FromDecimal_NegativeValue_SetsSignBitCorrectly()
    {
        // Arrange
        decimal input = -56.78m;

        // Act
        var result = DecimalValue.FromDecimal(input);

        // Assert
        result.Scale.Should().Be(2);
        result.Units.Should().Be(-5678);
    }

    [Test]
    public void FromDecimal_WholeNumber_ReturnsScaleZero()
    {
        // Arrange
        decimal input = 100m;

        // Act
        var result = DecimalValue.FromDecimal(input);

        // Assert
        result.Scale.Should().Be(0);
        result.Units.Should().Be(100);
    }

    [Test]
    public void FromDecimal_VerySmallDecimal_ReturnsHighScale()
    {
        // Arrange
        decimal input = 0.0000123m;

        // Act
        var result = DecimalValue.FromDecimal(input);

        // Assert
        result.Scale.Should().BeGreaterThan(0);
        result.ToDecimal().Should().BeApproximately(input, 0.0000001m);
    }

    [Test]
    public void RoundTrip_FromDecimalAndBack_ShouldPreserveValue()
    {
        // Arrange
        decimal original = 123.456m;

        // Act
        var intermediate = DecimalValue.FromDecimal(original);
        var roundTrip = intermediate.ToDecimal();

        // Assert
        roundTrip.Should().Be(original);
    }

    [Test]
    public void RoundTrip_NegativeDecimal_ShouldPreserveValue()
    {
        // Arrange
        decimal original = -9876.54321m;

        // Act
        var dv = DecimalValue.FromDecimal(original);
        var back = dv.ToDecimal();

        // Assert
        back.Should().Be(original);
    }
}
