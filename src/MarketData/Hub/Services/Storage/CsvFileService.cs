namespace QuantLab.MarketData.Hub.Services.Storage;

using System.IO;
using System.Text;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

public class CsvFileService(
    IOptions<FileStorageSettings> fileStorageSettings,
    ILogger<CsvFileService> logger
) : ICsvFileService
{
    private readonly string _directory = fileStorageSettings.Value.Directory;
    private readonly ILogger<CsvFileService> _logger = logger;

    public async Task WriteAsync<T>(
        string fileName,
        IEnumerable<T> records,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(records);

        _logger.LogInformation(
            "Writing CSV file to {fileName} with {Count} records",
            fileName,
            records.Count()
        );

        // Using UTF8 without BOM, newline for each record.
        // For simplicity assume T has simple flat properties.
        var properties = typeof(T).GetProperties();
        var header = string.Join(",", properties.Select(p => p.Name));

        var sb = new StringBuilder();
        sb.AppendLine(header);

        foreach (var record in records)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(record);
                return value is null ? "" : value.ToString()!;
            });
            sb.AppendLine(string.Join(",", values));
        }

        var filePath = Path.Combine(_directory, fileName);
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    public async Task<IEnumerable<T>> ReadAsync<T>(
        string fileName,
        Func<string[], T> mapFunc,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(mapFunc);

        _logger.LogInformation("Reading CSV file from {fileName}", fileName);

        var filePath = Path.Combine(_directory, fileName);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("CSV file not found at {filePath}", filePath);
            return [];
        }

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length <= 1)
        {
            // empty or just header
            return [];
        }

        // skip header row
        var dataLines = lines.Skip(1);
        var result = new List<T>();
        foreach (var line in dataLines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // simple split on comma – real CSV may require full parser
            var columns = line.Split(',');
            var item = mapFunc(columns);
            result.Add(item);
        }
        return result;
    }
}
