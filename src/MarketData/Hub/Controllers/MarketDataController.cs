using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Fetch;

namespace QuantLab.MarketData.Hub.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MarketDataController(IMarketDataFetchService marketDataFetchService)
    : ControllerBase
{
    [HttpGet("{symbol}/{interval}")]
    public async Task<IActionResult> GetMarketData(string symbol, string interval)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required");

        if (string.IsNullOrWhiteSpace(interval))
            return BadRequest("Interval is required");

        if (!BarIntervalConverter.TryParse(interval, out var barInterval))
            return BadRequest("Interval is invalid to fetch");

        var bars = await marketDataFetchService.GetMarketDataAsync(symbol, barInterval);
        var message = $"Fetched {bars.Count} records for {interval} interval of {symbol}";
        return Ok(new { Message = message, Bars = bars });
    }
}
