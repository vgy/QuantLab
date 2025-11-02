using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;

namespace QuantLab.MarketData.Hub.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DataController(IMarketDataFetchService marketDataFetchService) : ControllerBase
{
    [HttpGet("{symbol}/{barInterval}")]
    public async Task<IActionResult> GetData(string symbol, string barInterval)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        if (string.IsNullOrWhiteSpace(barInterval))
            return BadRequest("Bar Interval is required.");

        var result = BarInterval.GetFetchInterval(barInterval);
        if (!result.IsValid)
            return BadRequest("Bar Interval is invalid to fetch.");

        var message = await marketDataFetchService.GetDataAsync(symbol, result.BarInterval!);
        return Ok(new { message });
    }
}
