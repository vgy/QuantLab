using Grpc.Core;

namespace QuantLab.MarketData.Hub.Grpc;

using QuantLab.MarketData.Hub.Services;

public class JobGrpcService(
    IBackgroundJobQueue jobQueue,
    IMarketDataService marketDataService,
    ILogger<JobGrpcService> logger)
    : JobService.JobServiceBase
{
    public override Task<JobReply> StartJob(Empty request, ServerCallContext context)
    {
        jobQueue.QueueBackgroundWorkItem(async token =>
        {
            logger.LogInformation("Job started at {Time}", DateTimeOffset.Now);
            for (var i = 1; i <= 5; i++)
            {
                if (token.IsCancellationRequested) break;
                logger.LogInformation("Processing step {Step}/5...", i);
                await Task.Delay(2000, token);
            }
            logger.LogInformation("Job completed at {Time}", DateTimeOffset.Now);
        });

        return Task.FromResult(new JobReply { Message = "Background job started." });
    }

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

    public override Task<MarketDataStatusReply> GetMarketDataStatus(Empty request, ServerCallContext context)
    {
        var s = marketDataService.Status;
        return Task.FromResult(new MarketDataStatusReply
        {
            IsRunning = s.IsRunning,
            LastFetchTime = s.LastFetchTime?.ToString("O") ?? "",
            LastError = s.LastError ?? "",
            LastResponseSnippet = s.LastResponseSnippet ?? "",
            FetchCount = s.FetchCount
        });
    }
}