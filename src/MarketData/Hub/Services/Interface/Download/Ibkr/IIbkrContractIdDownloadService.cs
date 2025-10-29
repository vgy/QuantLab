namespace QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

public interface IIbkrContractIdDownloadService
{
    Task<string> DownloadContractIdsAsync(string file);
}
