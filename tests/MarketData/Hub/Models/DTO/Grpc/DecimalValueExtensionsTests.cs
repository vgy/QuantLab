namespace QuantLab.MarketData.Hub.UnitTests.Models.DTO.Grpc;

using FluentAssertions;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.DTO.Grpc;
using DecimalValueGrPc = QuantLab.Protos.MarketData.DecimalValue;

[TestFixture]
public class DecimalValueExtensionsTests
{
    [TestCase(123456789, 2)]
    [TestCase(987654321, 4)]
    [TestCase(456789123, 10)]
    [TestCase(456123789, 6)]
    public void ToProto_ValidDomainValue_ShouldConvertCorrectly(long units, int scale)
    {
        // Arrange
        var value = new DecimalValue(units, scale);

        // Act
        var proto = value.ToProto();

        // Assert
        proto.Units.Should().Be(units);
        proto.Scale.Should().Be(scale);
    }

    [TestCase(123456789, 2)]
    [TestCase(987654321, 4)]
    [TestCase(456789123, 10)]
    [TestCase(456123789, 6)]
    public void ToDomain_ValidProtoValue_ShouldConvertCorrectly(long units, int scale)
    {
        // Arrange
        var proto = new DecimalValueGrPc { Units = units, Scale = scale };

        // Act
        var result = proto.ToDomain();

        // Assert
        result.Units.Should().Be(units);
        result.Scale.Should().Be(scale);
    }

    [TestCase(123456789, 2)]
    [TestCase(987654321, 4)]
    [TestCase(456789123, 10)]
    [TestCase(456123789, 6)]
    public void ToDomain_ToProto_RoundTrip_ShouldPreserveValues(long units, int scale)
    {
        // Arrange
        var original = new DecimalValue(units, scale);

        // Act
        var roundTrip = original.ToProto().ToDomain();

        // Assert
        roundTrip.Units.Should().Be(original.Units);
        roundTrip.Scale.Should().Be(original.Scale);
    }
}
