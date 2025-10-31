namespace QuantLab.MarketData.Hub.Services.Download.Ibkr;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services.Interface.Download;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

public sealed class IbkrContractIdDownloadService(
    IDownloadQueue<ResponseData> downloadQueue,
    IServiceProvider serviceProvider,
    ICsvFileService fileService,
    IOptions<FileStorageSettings> fileStorageSettings,
    ILogger<IbkrContractIdDownloadService> logger
) : IIbkrContractIdDownloadService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<IbkrContractIdDownloadService> _logger = logger;
    private readonly string _symbolsAndContractIdsFileName = fileStorageSettings
        .Value
        .SymbolsAndContractIdsFileName;

    public async Task<string> DownloadContractIdsAsync(string file)
    {
        var symbols = await fileService.ReadAsync(file, a => a[0]);

        using var scope = _serviceProvider.CreateScope();
        var ibkrDownloadService = scope.ServiceProvider.GetRequiredService<IbkrDownloadService>();
        var tasks = new List<Task<ResponseData>>();
        foreach (var symbol in symbols)
        {
            var path = $"v1/api/trsrv/futures?symbols={symbol}&exchange=NSE";
            var task = downloadQueue.QueueAsync(async token =>
            {
                _logger.LogInformation("📥 Queued job for {symbol}", symbol);
                return await ibkrDownloadService.DownloadAsync(symbol, path, token);
            });

            tasks.Add(task);
        }

        _logger.LogInformation("✅ All {count} jobs queued", tasks.Count);

        var responseDatas = await Task.WhenAll(tasks);

        var results = responseDatas
            .Select(ParseResponseData)
            .OfType<Symbol>() // filters nulls
            .ToList();

        await fileService.WriteAsync(_symbolsAndContractIdsFileName, results);
        return $"Retrieved Contract Ids for {results.Count} of {symbols.Count()} symbols";
    }

    private Symbol? ParseResponseData(ResponseData responseData)
    {
        _logger.LogInformation(
            "✅ Received result for {symbol}, data count: {count}",
            responseData.Symbol,
            responseData.Data.Count
        );
        if (responseData.Data.Count == 0)
        {
            _logger.LogError("Parse error for {symbol}: Data.Count is 0", responseData.Symbol);
            return null;
        }

        try
        {
            var jsonElement = (JsonElement)responseData.Data[responseData.Symbol];
            var conid = jsonElement[0].GetProperty("conid").GetInt32();
            return new Symbol(responseData.Symbol, conid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parse error for {symbol}", responseData.Symbol);
        }
        return null;
    }
}
