namespace QuantLab.MarketData.Hub.Controllers;

using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Services;

[ApiController]
[Route("api/[controller]")]
public class JobController(
    IBackgroundJobQueue jobQueue,
    IMarketDataService marketDataService,
    ILogger<JobController> logger) : ControllerBase
{
    [HttpPost("start-job")]
    public IActionResult StartJob()
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

        return Accepted(new { message = "Background job started." });
    }

    [HttpPost("marketdata/start")]
    public IActionResult StartMarketDataJob()
    {
        marketDataService.Start();
        return Accepted(new { message = "Market data job started." });
    }

    [HttpPost("marketdata/stop")]
    public IActionResult StopMarketDataJob()
    {
        marketDataService.Stop();
        return Ok(new { message = "Market data job stopped." });
    }

    [HttpGet("marketdata/status")]
    public IActionResult GetMarketDataStatus() => Ok(marketDataService.Status);
}