namespace QuantLab.MarketData.Hub.Services.Interface;

using QuantLab.MarketData.Hub.Models.DTO.Responses;

public interface IIbkrDataDownloader
{
    Task<ResponseData> DownloadRecordAsync(
        string symbol,
        string url,
        CancellationToken cancellationToken = default
    );
}
