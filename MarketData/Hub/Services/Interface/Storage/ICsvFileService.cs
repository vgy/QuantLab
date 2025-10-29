namespace QuantLab.MarketData.Hub.Services.Interface.Storage;

public interface ICsvFileService
{
    Task WriteAsync<T>(
        string fileName,
        IEnumerable<T> records,
        CancellationToken cancellationToken = default
    );
    Task<IEnumerable<T>> ReadAsync<T>(
        string fileName,
        Func<string[], T> mapFunc,
        CancellationToken cancellationToken = default
    );
}
