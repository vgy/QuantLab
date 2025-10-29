namespace QuantLab.MarketData.Hub.Controllers;

using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Interface;

[ApiController]
[Route("api/[controller]")]
public class JobController(IMarketDataService marketDataService, IIbkrDataService ibkrDataService)
    : ControllerBase
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
        var message = await ibkrDataService.DownloadContractIdsAsync("symbols.csv");
        return Ok(new { message });
    }
}
