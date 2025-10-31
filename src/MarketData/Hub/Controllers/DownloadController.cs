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

    [HttpPost("bars/{barIntervalParam}")]
    public async Task<IActionResult> DownloadHistoricalBar(string barIntervalParam)
    {
        return await DownloadHistoricalBar(
            barIntervalParam,
            fileStorageSettings.Value.SymbolsAndContractIdsFileName
        );
    }

    [HttpPost("bars/{barIntervalParam}/retry")]
    public async Task<IActionResult> DownloadHistoricalBarForRetrySymbols(string barIntervalParam)
    {
        return await DownloadHistoricalBar(
            barIntervalParam,
            fileStorageSettings.Value.RetrySymbolsAndContractIdsFileName
        );
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
