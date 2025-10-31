namespace QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

using QuantLab.MarketData.Hub.Models.Domain;

public interface IIbkrBarDownloadService
{
    Task<string> DownloadHistoricalBarAsync(BarInterval barInterval, string inputFileName);
}
