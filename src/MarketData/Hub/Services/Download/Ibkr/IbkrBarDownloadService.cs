namespace QuantLab.MarketData.Hub.Services.Download.Ibkr;

using System.Text.Json;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Infrastructure.Time;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services.Interface.Download;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

public sealed class IbkrBarDownloadService(
    IDownloadQueue<ResponseData> downloadQueue,
    IServiceProvider serviceProvider,
    ICsvFileService fileService,
    ITimeProvider timeProvider,
    IOptions<FileStorageSettings> fileStorageSettings,
    IOptions<IbkrApiSettings> ibkrApiSettings,
    ILogger<IbkrContractIdDownloadService> logger
) : IIbkrBarDownloadService
{
    private readonly IDownloadQueue<ResponseData> _downloadQueue = downloadQueue;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ICsvFileService _fileService = fileService;
    private readonly ITimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IbkrContractIdDownloadService> _logger = logger;
    private readonly string _retrySymbolsAndContractIdsFileName = fileStorageSettings
        .Value
        .RetrySymbolsAndContractIdsFileName;
    private readonly string _historicalBarsRelativePathTemplate = fileStorageSettings
        .Value
        .HistoricalBarsRelativePathTemplate;
    private readonly string _historicalMarketDataEndPoint = ibkrApiSettings
        .Value
        .HistoricalMarketDataEndPoint;
    private const string Exchange = "NSE";

    public async Task<string> DownloadHistoricalBarAsync(
        BarInterval barInterval,
        string inputFileName
    )
    {
        var startTime = _timeProvider.Now;
        var symbols = await _fileService.ReadAsync(
            inputFileName,
            a => new Symbol(a[0], int.Parse(a[1]))
        );

        var symbolUrlPairs = symbols
            .Select(sc =>
                (Symbol: sc.Name, Path: BuildUrl(sc.CurrentFuturesContractId, barInterval))
            )
            .ToList();

        using var scope = _serviceProvider.CreateScope();
        var ibkrDownloadService = scope.ServiceProvider.GetRequiredService<IbkrDownloadService>();
        var tasks = new List<Task<ResponseData>>();
        ResponseData[] responseDatas = [];

        try
        {
            foreach (var pair in symbolUrlPairs)
            {
                var task = _downloadQueue.QueueAsync(async token =>
                {
                    _logger.LogInformation(
                        "📥 Queued job for {symbol} for {path}",
                        pair.Symbol,
                        pair.Path
                    );
                    return await ibkrDownloadService.DownloadAsync(pair.Symbol, pair.Path, token);
                });

                tasks.Add(task);
            }

            _logger.LogInformation("✅ All {count} jobs queued", tasks.Count);

            responseDatas = await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"{ex.Message}. One or more downloads failed. Continuing with available data."
            );
            // Collect completed tasks safely
            responseDatas =
            [
                .. tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Select(t => t.Result),
            ];
        }

        var results = responseDatas
            .Select(r => ParseResponseData(barInterval, r))
            .Where(x => x.Count > 0)
            .ToList();

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
            await _fileService.WriteAsync(relativePath, result);
        }

        var matchedKeys = results.Select(t => t[0].Symbol).ToHashSet();
        var unavailableSymbols = symbols.Where(x => !matchedKeys.Contains(x.Name)).ToList();
        await _fileService.WriteAsync(_retrySymbolsAndContractIdsFileName, unavailableSymbols);
        var endTime = _timeProvider.Now;
        _logger.LogInformation(
            $" DownloadHistoricalBarAsync for {barInterval.ToShortString()} - Started: {startTime:yyyy-MM-dd HH:mm:ss}, Ended: {endTime:yyyy-MM-dd HH:mm:ss}"
        );
        return $"{startTime:yyyy-MM-dd HH:mm:ss}: Retrieved Historical Bars of {barInterval.ToShortString()} for {results.Count} of {symbols.Count()} symbols";
    }

    private string BuildUrl(int conId, BarInterval barInterval, string? startTime = null)
    {
        (string period, string bar) = barInterval switch
        {
            BarInterval.FiveMinutes => ("1w", "5min"),
            BarInterval.FifteenMinutes => ("1m", "15min"),
            BarInterval.OneHour => ("1m", "1h"),
            BarInterval.OneDay => ("1y", "1d"),
            _ => ("1y", "1d"),
        };

        var query = new Dictionary<string, string>
        {
            { "conid", conId.ToString() },
            { "exchange", Exchange },
            { "period", period },
            { "bar", bar },
        };

        if (!string.IsNullOrEmpty(startTime))
            query["startTime"] = startTime;

        var queryString = string.Join(
            "&",
            query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}")
        );

        return $"{_historicalMarketDataEndPoint}?{queryString}";
    }

    private List<Bar> ParseResponseData(BarInterval barInterval, ResponseData responseData)
    {
        var symbol = responseData.Symbol;
        _logger.LogInformation(
            "✅ Received result for {symbol}, data count: {count}",
            symbol,
            responseData.Data.Count
        );

        if (responseData.Data.Count == 0)
        {
            _logger.LogError("No data for {symbol}: ", symbol);
            return [];
        }

        Object data = new();
        try
        {
            var response = responseData.Data;
            if (response is null || !response.TryGetValue("data", out data!))
            {
                _logger.LogError("Parse error for {symbol}: No valid data", symbol);
                return [];
            }

            var jsonData = (JsonElement)data;

            List<Bar> bars =
            [
                .. jsonData
                    .EnumerateArray()
                    .Select(item => new Bar(
                        symbol,
                        barInterval,
                        ToIST(item.GetProperty("t").GetInt64()),
                        item.GetProperty("o").GetDecimal(),
                        item.GetProperty("h").GetDecimal(),
                        item.GetProperty("l").GetDecimal(),
                        item.GetProperty("c").GetDecimal(),
                        item.GetProperty("v").GetInt32()
                    )),
            ];
            if (bars.Count == 0)
            {
                _logger.LogError("No bar for {symbol}", symbol);
            }
            return bars;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parse error for {symbol}", symbol);
        }
        return [];
    }

    private static string ToIST(long unixMs)
    {
        DateTimeOffset utcTime = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
        TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        DateTime istTime = TimeZoneInfo.ConvertTime(utcTime, istZone).DateTime;
        return istTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
