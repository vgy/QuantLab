namespace QuantLab.MarketData.Hub.Controllers;

using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[ApiController]
[Route("api/[controller]")]
public class JobController(
    IMarketDataService marketDataService,
    IIbkrContractIdDownloadService ibkrContractIdDownloadService
) : ControllerBase
{
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

    [HttpGet("data/download/contractids")]
    public async Task<IActionResult> DownloadContractIds()
    {
        var message = await ibkrContractIdDownloadService.DownloadContractIdsAsync("symbols.csv");
        return Ok(new { message });
    }
}
