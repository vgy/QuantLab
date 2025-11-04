using Grpc.Core;

namespace QuantLab.MarketData.Hub.Grpc;

using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.Protos.MarketData;

public class DownloadGrpcService(
    IIbkrContractIdDownloadService ibkrContractIdDownloadService,
    IIbkrBarDownloadService ibkrBarDownloadService,
    IOptions<FileStorageSettings> fileStorageSettings
) : DownloadGrpc.DownloadGrpcBase
{
    public override async Task<StatusReply> DownloadContractIds(
        Empty request,
        ServerCallContext context
    )
    {
        var message = await ibkrContractIdDownloadService.DownloadContractIdsAsync(
            fileStorageSettings.Value.SymbolsFileName
        );
        return new StatusReply { Message = message };
    }

    public override async Task<StatusReply> DownloadHistoricalBars(
        HistoricalBarsRequest historicalBarsRequest,
        ServerCallContext context
    )
    {
        return await DownloadHistoricalBars(
            historicalBarsRequest,
            fileStorageSettings.Value.SymbolsAndContractIdsFileName
        );
    }

    public override async Task<StatusReply> DownloadHistoricalBarsForMissedSymbols(
        HistoricalBarsRequest historicalBarsRequest,
        ServerCallContext context
    )
    {
        return await DownloadHistoricalBars(
            historicalBarsRequest,
            fileStorageSettings.Value.RetrySymbolsAndContractIdsFileName
        );
    }

    private async Task<StatusReply> DownloadHistoricalBars(
        HistoricalBarsRequest historicalBarsRequest,
        string inputFileName
    )
    {
        var interval = historicalBarsRequest.Interval;
        if (string.IsNullOrWhiteSpace(interval))
            return new StatusReply { Message = "Interval is required" };

        if (!BarIntervalConverter.TryParse(interval, out var barInterval))
            return new StatusReply { Message = "Interval is invalid" };

        var message = await ibkrBarDownloadService.DownloadHistoricalBarAsync(
            barInterval,
            inputFileName
        );
        return new StatusReply { Message = message };
    }
}
