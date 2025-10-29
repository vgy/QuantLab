namespace QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

using QuantLab.MarketData.Hub.Models.DTO.Responses;

public interface IIbkrDownloadService
{
    Task<ResponseData> DownloadAsync(
        string symbol,
        string url,
        CancellationToken cancellationToken = default
    );
}
