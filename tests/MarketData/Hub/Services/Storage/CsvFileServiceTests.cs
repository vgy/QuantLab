namespace QuantLab.MarketData.Hub.UnitTests.Services.Storage;

using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Storage;

public class CsvFileServiceTests
{
    private CsvFileService _service = default!;
    private Mock<IOptions<FileStorageSettings>> _fileStorageSettingsMock = null!;
    private Mock<ILogger<CsvFileService>> _mockLogger = default!;
    private readonly string tempFilePath = Path.GetTempPath();

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new();
        _fileStorageSettingsMock = new();
        var fileStorageSettings = new FileStorageSettings { Directory = tempFilePath };
        _fileStorageSettingsMock.Setup(x => x.Value).Returns(fileStorageSettings);
        _service = new CsvFileService(_fileStorageSettingsMock.Object, _mockLogger.Object);
    }

    [Test]
    public void WriteAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        string? filePath = null;
        var records = new[] { new { A = 1, B = "x" } };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.WriteAsync(filePath!, records)
        );
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    public void WriteAsync_EmptyFilePath_ThrowsArgumentException(string fileName)
    {
        // Arrange
        var records = new[] { new { A = 1, B = "x" } };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.WriteAsync(fileName, records)
        );
    }

    [Test]
    public void WriteAsync_NullRecords_ThrowsArgumentNullException()
    {
        // Arrange
        string filePath = Path.Combine(tempFilePath, "test.csv");
        object[]? records = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.WriteAsync(filePath, records!)
        );
    }

    [Test]
    public async Task WriteAsync_ValidRecords_CreatesFileWithExpectedContent()
    {
        // Arrange
        string filePath = Path.Combine(tempFilePath, $"{Guid.NewGuid()}.csv");
        try
        {
            var records = new[]
            {
                new TestRecord { A = 1, B = "hello" },
                new TestRecord { A = 2, B = "world" },
            };

            // Act
            await _service.WriteAsync(filePath, records);

            // Assert
            Assert.That(File.Exists(filePath), Is.True, "File should have been created");
            var text = await File.ReadAllTextAsync(filePath);
            var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines.Length, Is.EqualTo(3)); // header + 2 records
            Assert.That(lines[0], Is.EqualTo("A,B"));
            Assert.That(lines[1], Is.EqualTo("1,hello"));
            Assert.That(lines[2], Is.EqualTo("2,world"));
        }
        finally
        {
            // cleanup
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Test]
    public void ReadAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        string? filePath = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.ReadAsync<TestRecord>(filePath!, cols => new TestRecord())
        );
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    public void ReadAsync_EmptyFilePath_ThrowsArgumentException(string fileName)
    {
        // Arrange & Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.ReadAsync<TestRecord>(fileName, cols => new TestRecord())
        );
    }

    [Test]
    public void ReadAsync_NullMapFunc_ThrowsArgumentNullException()
    {
        // Arrange
        string filePath = Path.Combine(tempFilePath, $"{Guid.NewGuid()}.csv");

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.ReadAsync<TestRecord>(filePath, null!)
        );
    }

    [Test]
    public async Task ReadAsync_FileNotExists_ReturnsEmpty()
    {
        // Arrange
        string filePath = Path.Combine(tempFilePath, $"{Guid.NewGuid()}.csv");
        // Ensure file does not exist
        if (File.Exists(filePath))
            File.Delete(filePath);

        // Act
        var result = await _service.ReadAsync<TestRecord>(filePath, cols => new TestRecord());

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task ReadAsync_FileExists_ReturnsMappedRecords()
    {
        // Arrange
        string filePath = Path.Combine(tempFilePath, $"{Guid.NewGuid()}.csv");
        try
        {
            // Create a file manually
            var lines = new[] { "A,B", "1,foo", "2,bar" };
            await File.WriteAllLinesAsync(filePath, lines);

            // Act
            var result = await _service.ReadAsync<TestRecord>(
                filePath,
                cols =>
                {
                    return new TestRecord { A = int.Parse(cols[0]), B = cols[1] };
                }
            );

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].A, Is.EqualTo(1));
            Assert.That(list[0].B, Is.EqualTo("foo"));
            Assert.That(list[1].A, Is.EqualTo(2));
            Assert.That(list[1].B, Is.EqualTo("bar"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private class TestRecord
    {
        public int A { get; set; }
        public string B { get; set; } = default!;
    }
}
