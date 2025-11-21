namespace QuantLab.MarketData.Hub.UnitTests.Models.Domain;

using System.Text.Json;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Domain;

[TestFixture]
public class BarIntervalJsonConverterTests
{
    private BarIntervalJsonConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new BarIntervalJsonConverter();
    }

    [TestCase("5min")]
    [TestCase("15min")]
    [TestCase("1h")]
    [TestCase("1d")]
    public void Write_GivenOneMinuteInterval_WritesCorrectShortString(string interval)
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        _ = BarIntervalConverter.TryParse(interval, out var barInterval);
        var value = barInterval;

        // Act
        _converter.Write(writer, value, new JsonSerializerOptions());
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        Assert.That(json, Is.EqualTo($"\"{interval}\""));
    }

    [Test]
    public void Read_Always_ThrowsNotImplementedException()
    {
        // Arrange
        var json = "\"1m\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));

        // Act & Assert
        try
        {
            _converter.Read(ref reader, typeof(BarInterval), new JsonSerializerOptions());
            Assert.Fail("Expected NotImplementedException was not thrown.");
        }
        catch (NotImplementedException)
        {
            // Test passes
        }
    }
}
