namespace QuantLab.MarketData.Hub.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services.Interface;
using QuantLab.MarketData.Hub.Services.Interface.Storage;

public sealed class IbkrDataService(
    IBackgroundJobQueue<ResponseData> jobQueue,
    IServiceProvider serviceProvider,
    ICsvFileService fileService,
    ILogger<IbkrDataService> logger
) : IIbkrDataService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<IbkrDataService> logger = logger;

    public async Task<string> DownloadContractIdsAsync(string file)
    {
        var symbols = await fileService.ReadAsync(file, a => a[0]);

        using var scope = serviceProvider.CreateScope();
        var ibkrDataDownloader = scope.ServiceProvider.GetRequiredService<IbkrDataDownloader>();
        var tasks = new List<Task<ResponseData>>();
        foreach (var symbol in symbols)
        {
            var path = $"v1/api/trsrv/futures?symbols={symbol}&exchange=NSE";
            var task = jobQueue.QueueAsync(async token =>
            {
                logger.LogInformation("📥 Queued job for {symbol}", symbol);
                return await ibkrDataDownloader.DownloadRecordAsync(symbol, path, token);
            });

            tasks.Add(task);
        }

        logger.LogInformation("✅ All {count} jobs queued", tasks.Count);

        var responseDatas = await Task.WhenAll(tasks);

        var results = responseDatas
            .Select(ParseResponseData)
            .OfType<Symbol>() // filters nulls
            .ToList();

        await fileService.WriteAsync("symbols_contractIds.csv", results);
        return $"Retrieved Contract Ids for {results.Count} of {symbols.Count()} symbols";
    }

    private Symbol? ParseResponseData(ResponseData responseData)
    {
        logger.LogInformation(
            "✅ Received result for {symbol}, data count: {count}",
            responseData.Symbol,
            responseData.Data.Count
        );
        if (responseData.Data.Count == 0)
        {
            logger.LogError("Parse error for {symbol}: Data.Count is 0", responseData.Symbol);
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
            logger.LogError(ex, "Parse error for {symbol}", responseData.Symbol);
        }
        return null;
    }
}
