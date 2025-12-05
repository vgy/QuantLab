namespace QuantLab.MarketData.Hub.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.Domain;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[ApiController]
[Route("api/[controller]")]
public class DownloadController(
    IIbkrContractIdDownloadService ibkrContractIdDownloadService,
    IIbkrBarDownloadService ibkrBarDownloadService,
    IIbkrTwsBarDownloadService ibkrTwsBarDownloadService,
    IOptions<FileStorageSettings> fileStorageSettings
) : ControllerBase
{
    [HttpPost("contractids")]
    public async Task<IActionResult> DownloadContractIds()
    {
        var message = await ibkrContractIdDownloadService.DownloadContractIdsAsync(
            fileStorageSettings.Value.SymbolsFileName
        );
        return Ok(new { message });
    }

    [HttpPost("bars/{interval}")]
    public async Task<IActionResult> DownloadHistoricalBars(string interval)
    {
        return await DownloadHistoricalBars(
            interval,
            fileStorageSettings.Value.SymbolsAndContractIdsFileName
        );
    }

    [HttpPost("bars/{interval}/retry")]
    public async Task<IActionResult> DownloadHistoricalBarsForMissedSymbols(string interval)
    {
        return await DownloadHistoricalBars(
            interval,
            fileStorageSettings.Value.RetrySymbolsAndContractIdsFileName
        );
    }

    private async Task<IActionResult> DownloadHistoricalBars(string interval, string inputFileName)
    {
        if (string.IsNullOrWhiteSpace(interval))
            return BadRequest("Interval is required");

        if (!BarIntervalConverter.TryParse(interval, out var barInterval))
            return BadRequest("Interval is invalid");

        var message = await ibkrBarDownloadService.DownloadHistoricalBarAsync(
            barInterval,
            inputFileName
        );
        return Ok(new { message });
    }

    [HttpPost("tws/bars/{interval}")]
    public async Task<IActionResult> DownloadTwsHistoricalBars(string interval)
    {
        return await DownloadTwsHistoricalBars(
            interval,
            fileStorageSettings.Value.SymbolsAndContractIdsFileName
        );
    }

    [HttpPost("tws/bars/{interval}/retry")]
    public async Task<IActionResult> DownloadTwsHistoricalBarsForMissedSymbols(string interval)
    {
        return await DownloadTwsHistoricalBars(
            interval,
            fileStorageSettings.Value.RetrySymbolsAndContractIdsFileName
        );
    }

    private async Task<IActionResult> DownloadTwsHistoricalBars(
        string interval,
        string inputFileName
    )
    {
        if (string.IsNullOrWhiteSpace(interval))
            return BadRequest("Interval is required");

        if (!BarIntervalConverter.TryParse(interval, out var barInterval))
            return BadRequest("Interval is invalid");

        var message = await ibkrTwsBarDownloadService.DownloadTwsHistoricalBarAsync(
            barInterval,
            inputFileName
        );
        return Ok(new { message });
    }
}
