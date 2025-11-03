using Grpc.Core;

namespace QuantLab.MarketData.Hub.Grpc;

using System.Runtime.InteropServices;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;
using QuantLab.Protos.MarketData;

public class MarketDataGrpcService(IMarketDataFetchService marketDataFetchService)
    : MarketDataGrpc.MarketDataGrpcBase
{
    public override async Task<MarketDataResponse> GetMarketData(
        MarketDataRequest request,
        ServerCallContext context
    )
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
            return new MarketDataResponse { Message = "Symbol is required" };

        if (string.IsNullOrWhiteSpace(request.Interval))
            return new MarketDataResponse { Message = "Interval is required" };

        var result = BarInterval.GetFetchInterval(request.Interval);
        if (!result.IsValid)
            return new MarketDataResponse { Message = "Interval is invalid to fetch" };

        var bars = await marketDataFetchService.GetMarketDataAsync(
            request.Symbol,
            result.BarInterval!
        );
        var barsSpan = CollectionsMarshal.AsSpan([.. bars]);
        var response = new MarketDataResponse { Message = "Successfully fetched market data" };
        response.Bars.AddRangeFast(barsSpan);
        return response;
    }
}
