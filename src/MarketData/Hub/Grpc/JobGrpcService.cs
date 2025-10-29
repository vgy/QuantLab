using Grpc.Core;

namespace QuantLab.MarketData.Hub.Grpc;

using QuantLab.MarketData.Hub.Services;

public class JobGrpcService(IMarketDataService marketDataService) : JobService.JobServiceBase
{
    public override Task<JobReply> StartMarketDataJob(Empty request, ServerCallContext context)
    {
        marketDataService.Start();
        return Task.FromResult(new JobReply { Message = "Market data job started." });
    }

    public override Task<JobReply> StopMarketDataJob(Empty request, ServerCallContext context)
    {
        marketDataService.Stop();
        return Task.FromResult(new JobReply { Message = "Market data job stopped." });
    }

    public override Task<MarketDataStatusReply> GetMarketDataStatus(
        Empty request,
        ServerCallContext context
    )
    {
        var s = marketDataService.Status;
        return Task.FromResult(
            new MarketDataStatusReply
            {
                IsRunning = s.IsRunning,
                LastFetchTime = s.LastFetchTime?.ToString("O") ?? "",
                LastError = s.LastError ?? "",
                LastResponseSnippet = s.LastResponseSnippet ?? "",
                FetchCount = s.FetchCount,
            }
        );
    }
}
