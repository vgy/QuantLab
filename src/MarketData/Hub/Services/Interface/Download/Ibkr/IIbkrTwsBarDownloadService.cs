namespace QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

using QuantLab.MarketData.Hub.Models.Domain;

public interface IIbkrTwsBarDownloadService
{
    Task<string> DownloadTwsHistoricalBarAsync(BarInterval barInterval, string inputFileName);
}
