namespace QuantLab.MarketData.Hub.Models.Config;

public sealed class IbkrApiSettings
{
    public const string SectionName = "IbkrApi";
    public string BaseUrl { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "MyApp/1.0";
    public string Accept { get; set; } = "application/json";
    public bool BypassSsl { get; set; } = false;
}
