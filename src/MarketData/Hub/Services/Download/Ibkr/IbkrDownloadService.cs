namespace QuantLab.MarketData.Hub.Services.Download.Ibkr;

using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;

public class IbkrDownloadService(HttpClient httpClient, ILogger<IbkrDownloadService> logger)
    : IIbkrDownloadService
{
    private const int MaxRetries = 3;
    private static readonly HashSet<int> RetryStatuses = [429, 503];
    private static readonly Range RetryStatusRange = 500..600;

    public async Task<ResponseData> DownloadAsync(
        string symbol,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Requesting data for {symbol} (Attempt {attempt}): {path}",
                    symbol,
                    attempt,
                    path
                );

                using var response = await httpClient.GetAsync(path, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var data =
                        await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(
                            cancellationToken: cancellationToken
                        ) ?? [];
                    logger.LogInformation("✅ Success for {symbol}", symbol);
                    return new(symbol, data);
                }
                else if (ShouldRetry(response.StatusCode))
                {
                    logger.LogWarning(
                        "⚠️ Retriable error {statusCode} for {symbol} (Attempt {attempt})",
                        (int)response.StatusCode,
                        symbol,
                        attempt
                    );

                    if (attempt == MaxRetries)
                    {
                        logger.LogError(
                            "❌ Failed for {symbol} after {maxRetries} attempts (Status {statusCode})",
                            symbol,
                            MaxRetries,
                            (int)response.StatusCode
                        );
                        return new(symbol, []);
                    }

                    // Exponential backoff with cancellation support
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                else
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError(
                        "❌ Non-retriable failure for {symbol} (Status {statusCode}): {text}",
                        symbol,
                        (int)response.StatusCode,
                        text
                    );
                    return new(symbol, []);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("⏹️ Download cancelled for {symbol}", symbol);
                throw; // propagate cancellation properly
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "💥 Exception for {symbol} (Attempt {attempt})",
                    symbol,
                    attempt
                );
                return new(symbol, []);
            }
        }

        return new(symbol, []);
    }

    private static bool ShouldRetry(System.Net.HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return RetryStatuses.Contains(code)
            || (code >= RetryStatusRange.Start.Value && code < RetryStatusRange.End.Value);
    }
}
