using QuantLab.MarketData.Hub.Models.Domain;

namespace QuantLab.MarketData.Hub.Services.Interface.Fetch;

public interface IMarketDataFetchService
{
    Task<IReadOnlyList<Bar>> GetDataAsync(string symbol, BarInterval barInterval);
}
