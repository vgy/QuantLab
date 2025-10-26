namespace QuantLab.MarketData.Hub.Services;

using Microsoft.Extensions.Logging;

public interface IMarketDataService
{
    MarketDataStatus Status { get; }

    void Start();
    void Stop();
}

public sealed class MarketDataService(
    ILogger<MarketDataService> logger,
    IHttpClientFactory httpClientFactory) : IMarketDataService
{
    private readonly MarketDataStatus _status = new();
    private CancellationTokenSource? _cts;
    private Task? _runningTask;
    public MarketDataStatus Status => _status;

    public void Start()
    {
        if (_status.IsRunning)
        {
            logger.LogInformation("Market data job already running");
            return;
        }

        logger.LogInformation("Starting market data job...");
        _status.IsRunning = true;
        _status.LastError = null;
        _cts = new();

        _runningTask = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!_status.IsRunning) return;

        logger.LogInformation("Stopping market data job...");
        _cts?.Cancel();
        _status.IsRunning = false;
    }

    private async Task RunAsync(CancellationToken token)
    {
        var client = httpClientFactory.CreateClient();
        Random random = new();

        while (!token.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Fetching market data...");
                var num = random.Next(1, 100);
                if (num % 2 == 0)
                {
                    Stop();
                    return;
                }
                _status.LastResponseSnippet = "Job Finished Successfully";
                _status.LastFetchTime = DateTime.UtcNow;
                _status.FetchCount++;
                logger.LogInformation("Market data fetched successfully at {Time}", _status.LastFetchTime);

                // using var response = await client.GetAsync("https://sandbox.ibkr.com/api/v1/marketdata", token);

                // switch (response.StatusCode)
                // {
                // 	case HttpStatusCode.Unauthorized:
                // 		logger.LogWarning("Unauthorized response from IBKR API — stopping job.");
                // 		_status.LastError = "Unauthorized (401)";
                // 		Stop();
                // 		return;

                // 	case HttpStatusCode.OK:
                // 		var json = await response.Content.ReadAsStringAsync(token);
                // 		_status.LastResponseSnippet = json.Length > 80 ? $"{json[..80]}..." : json;
                // 		_status.LastFetchTime = DateTime.UtcNow;
                // 		_status.FetchCount++;
                // 		logger.LogInformation("Market data fetched successfully at {Time}", _status.LastFetchTime);
                // 		break;

                // 	default:
                // 		_status.LastError = $"Unexpected status: {(int)response.StatusCode} {response.ReasonPhrase}";
                // 		logger.LogWarning("Unexpected status code: {StatusCode}", response.StatusCode);
                // 		break;
                // }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching market data.");
                _status.LastError = ex.Message;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Market data job stopped.");
    }
}
