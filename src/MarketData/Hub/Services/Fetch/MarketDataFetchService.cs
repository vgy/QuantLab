namespace QuantLab.MarketData.Hub.Services.Fetch;

using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

public sealed class MarketDataFetchService(
    ICsvFileService fileService,
    IOptions<FileStorageSettings> fileStorageSettings,
    ILogger<MarketDataFetchService> logger
) : IMarketDataFetchService
{
    private readonly ICsvFileService fileService = fileService;
    private readonly string _historicalBarsRelativePathTemplate = fileStorageSettings
        .Value
        .HistoricalBarsRelativePathTemplate;
    private readonly ILogger<MarketDataFetchService> logger = logger;

    public async Task<IReadOnlyList<Bar>> GetDataAsync(string symbol, BarInterval barInterval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(barInterval);
        var fileName = string.Format(_historicalBarsRelativePathTemplate, barInterval, symbol);
        try
        {
            var bars = await fileService.ReadAsync(fileName, a => ParseBar(a, fileName));
            List<Bar> barsList = [.. bars];
            logger.LogInformation(
                "Fetched {count} records for {barInterval} of {symbol} from {fileName}",
                barsList.Count,
                barInterval,
                symbol,
                fileName
            );
            return barsList;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Data Fetch Error for {barInterval} of {symbol} from {fileName}",
                barInterval,
                symbol,
                fileName
            );
        }
        return [];
    }

    private static Bar ParseBar(string[] values, string fileName)
    {
        if (
            values.Length != 8
            || !BarInterval.TryParse(values[1], out BarInterval? interval)
            || !long.TryParse(values[2], out long timestamp)
            || !decimal.TryParse(values[3], out decimal open)
            || !decimal.TryParse(values[4], out decimal high)
            || !decimal.TryParse(values[5], out decimal low)
            || !decimal.TryParse(values[6], out decimal close)
            || !int.TryParse(values[7], out int volume)
        )
        {
            throw new ArgumentException(
                $"Parsing error in {string.Join(',', values)} of {fileName}"
            );
        }

        return new Bar
        {
            Symbol = values[0],
            Interval = interval!,
            Timestamp = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
        };
    }
}
