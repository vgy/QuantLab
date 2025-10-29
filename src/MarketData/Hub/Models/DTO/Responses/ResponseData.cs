namespace QuantLab.MarketData.Hub.Models.DTO.Responses;

public record struct ResponseData(string Symbol, Dictionary<string, object> Data);
