namespace QuantLab.MarketData.Hub.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class BackgroundWorkerService(
	IBackgroundJobQueue jobQueue,
	ILogger<BackgroundWorkerService> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Background worker started at {Time}", DateTimeOffset.Now);

		while (!stoppingToken.IsCancellationRequested)
		{
			var workItem = await jobQueue.DequeueAsync(stoppingToken);

			try
			{
				await workItem(stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred executing background job");
			}
		}

		logger.LogInformation("Background worker stopped at {Time}", DateTimeOffset.Now);
	}
}