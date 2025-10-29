namespace QuantLab.MarketData.Hub.Services.Interface;

using QuantLab.MarketData.Hub.Models.DTO.Responses;

public interface IIbkrDataService
{
    Task<string> DownloadContractIdsAsync(string file);
}
