namespace QuantLab.MarketData.Hub.Controllers;

using Microsoft.AspNetCore.Mvc;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[ApiController]
[Route("api/[controller]")]
public class DownloadController(
    IIbkrContractIdDownloadService ibkrContractIdDownloadService,
    IIbkrBarDownloadService ibkrBarDownloadService
) : ControllerBase
{
    [HttpPost("contractids")]
    public async Task<IActionResult> DownloadContractIds()
    {
        var message = await ibkrContractIdDownloadService.DownloadContractIdsAsync("symbols.csv");
        return Ok(new { message });
    }

    [HttpPost("bars/{barIntervalParam}")]
    public async Task<IActionResult> DownloadHistoricalBar(string barIntervalParam)
    {
        return await DownloadHistoricalBar(barIntervalParam, "symbols_contractIds.csv");
    }

    [HttpPost("bars/{barIntervalParam}/retry")]
    public async Task<IActionResult> DownloadHistoricalBarForRetrySymbols(string barIntervalParam)
    {
        return await DownloadHistoricalBar(barIntervalParam, "retry_symbols_contractIds.csv");
    }

    private async Task<IActionResult> DownloadHistoricalBar(
        string barIntervalParam,
        string inputFileName
    )
    {
        if (string.IsNullOrWhiteSpace(barIntervalParam))
            return BadRequest("Bar Interval parameter is required.");

        if (!BarInterval.TryParse(barIntervalParam, out var barInterval))
            return BadRequest("Bar Interval parameter is invalid.");

        var message = await ibkrBarDownloadService.DownloadHistoricalBarAsync(
            barInterval!,
            inputFileName
        );
        return Ok(new { message });
    }
}
