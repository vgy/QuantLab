namespace QuantLab.MarketData.Hub.Models.Config;

public sealed class FileStorageSettings
{
    public const string SectionName = "FileStorage";
    public string Directory { get; set; } = string.Empty;
    public string SymbolsFileName { get; set; } = string.Empty;
    public string SymbolsAndContractIdsFileName { get; set; } = string.Empty;
    public string RetrySymbolsAndContractIdsFileName { get; set; } = string.Empty;
    public string HistoricalBarsRelativePathTemplate { get; set; } = string.Empty;
}
