namespace QuantLab.MarketData.Hub.Services.Download.Ibkr;

using IBApi;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Infrastructure.Time;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Storage;
using QuantLabBar = QuantLab.MarketData.Hub.Models.Domain.Bar;

public sealed class IbkrTwsBarDownloadService(
    IServiceProvider serviceProvider,
    ICsvFileService fileService,
    ITimeProvider timeProvider,
    IOptions<FileStorageSettings> fileStorageSettings,
    ILogger<IbkrTwsBarDownloadService> logger
) : IIbkrTwsBarDownloadService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ICsvFileService _fileService = fileService;
    private readonly ITimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IbkrTwsBarDownloadService> _logger = logger;
    private readonly string _retrySymbolsAndContractIdsFileName = fileStorageSettings
        .Value
        .RetrySymbolsAndContractIdsFileName;
    private readonly string _historicalBarsRelativePathTemplate = fileStorageSettings
        .Value
        .HistoricalBarsRelativePathTemplate;

    public async Task<string> DownloadTwsHistoricalBarAsync(
        BarInterval barInterval,
        string inputFileName
    )
    {
        var startTime = _timeProvider.Now;
        var futuresContracts = await _fileService.ReadAsync(
            inputFileName,
            a => new FuturesContract(a[0], int.Parse(a[1]))
        );

        using var scope = _serviceProvider.CreateScope();
        var ibkrTwsService = scope.ServiceProvider.GetRequiredService<IbkrTwsService>();
        List<List<QuantLabBar>> results = [];

        try
        {
            foreach (var pair in futuresContracts)
            {
                _logger.LogInformation(
                    "📥 Queued job for {symbol} for {contractId}",
                    pair.Symbol,
                    pair.CurrentFuturesContractId
                );
                var contract = new Contract
                {
                    ConId = pair.CurrentFuturesContractId,
                    Symbol = pair.Symbol,
                    SecType = "FUT",
                    Currency = "INR",
                    Exchange = "NSE",
                };

                var result = await ibkrTwsService.GetTwsHistoricalDataAsync(
                    contract,
                    "1 D",
                    GetBarSizeSetting(barInterval)
                );
                results.Add(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"{ex.Message}. One or more downloads failed. Continuing with available data."
            );
        }

        foreach (var result in results)
        {
            if (result.Count == 0)
                continue;
            var bar = result[0];
            var relativePath = string.Format(
                _historicalBarsRelativePathTemplate,
                bar.Interval.ToShortString(),
                bar.Symbol
            );
            await AppendBars(relativePath, result);
        }

        var matchedKeys =
            (results.Count != 0)
                ? results.Where(t => t.Count != 0).Select(t => t[0].Symbol).ToHashSet()
                : [];
        var unavailableSymbols = futuresContracts
            .Where(x => !matchedKeys.Contains(x.Symbol))
            .ToList();
        await _fileService.WriteAsync(_retrySymbolsAndContractIdsFileName, unavailableSymbols);
        var endTime = _timeProvider.Now;
        _logger.LogInformation(
            $" DownloadTwsHistoricalBarAsync for {barInterval.ToShortString()} - Started: {startTime:yyyy-MM-dd HH:mm:ss}, Ended: {endTime:yyyy-MM-dd HH:mm:ss}"
        );
        return $"{startTime:yyyy-MM-dd HH:mm:ss}: Retrieved Historical Bars of {barInterval.ToShortString()} for {matchedKeys.Count} of {futuresContracts.Count()} symbols";
    }

    private static string GetBarSizeSetting(BarInterval barInterval)
    {
        return barInterval switch
        {
            BarInterval.FiveMinutes => "5 mins",
            BarInterval.FifteenMinutes => "15 mins",
            _ => "5 mins",
        };
    }

    private async Task AppendBars(string fileName, IList<QuantLabBar> barsToAppend)
    {
        List<QuantLabBar> bars = [];
        var records = await _fileService.ReadAsync(fileName, a => ParseBar(a, fileName));
        DateTime today = DateTime.Today;
        bars.AddRange([.. records.Where(r => DateTime.Parse(r.Timestamp).Date != today)]);
        bars.AddRange(barsToAppend);
        await _fileService.WriteAsync(fileName, bars);
    }

    private static QuantLabBar ParseBar(string[] values, string fileName)
    {
        if (
            values.Length != 8
            || !BarIntervalConverter.TryParse(values[1], out BarInterval interval)
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

        return new QuantLabBar
        {
            Symbol = values[0],
            Interval = interval,
            Timestamp = values[2],
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
        };
    }
}
