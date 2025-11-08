namespace QuantLab.MarketData.Hub.Benchmarks.Controllers;

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using QuantLab.MarketData.Hub.Controllers;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

[MemoryDiagnoser] // Measures memory allocations too
public class DownloadControllerBenchmarks
{
    private DownloadController? _controller;

    [GlobalSetup] // Runs once before benchmarking
    public void Setup()
    {
        var contractServiceMock = new Mock<IIbkrContractIdDownloadService>();
        contractServiceMock
            .Setup(s => s.DownloadContractIdsAsync(It.IsAny<string>()))
            .ReturnsAsync("Downloaded Successfully");

        // Mock IIbkrBarDownloadService
        var barServiceMock = new Mock<IIbkrBarDownloadService>();

        // File storage settings
        var settings = Options.Create(new FileStorageSettings { SymbolsFileName = "symbols.json" });

        _controller = new DownloadController(
            contractServiceMock.Object,
            barServiceMock.Object,
            settings
        );
    }

    [Benchmark]
    public async Task<IActionResult> DownloadContractIdsBenchmark()
    {
        return await _controller!.DownloadContractIds();
    }
}
