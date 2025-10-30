using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Grpc;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Download;
using QuantLab.MarketData.Hub.Services.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Download;
using QuantLab.MarketData.Hub.Services.Interface.Download.Ibkr;
using QuantLab.MarketData.Hub.Services.Interface.Storage;
using QuantLab.MarketData.Hub.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IbkrApiSettings>(
    builder.Configuration.GetSection(IbkrApiSettings.SectionName)
);
builder.Services.Configure<DownloadServiceSettings>(
    builder.Configuration.GetSection(DownloadServiceSettings.SectionName)
);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ICsvFileService, CsvFileService>();
builder.Services.AddSingleton(typeof(IDownloadQueue<>), typeof(DownloadQueue<>));
builder.Services.AddHostedService<DownloadBackgroundService<ResponseData>>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IIbkrContractIdDownloadService, IbkrContractIdDownloadService>();

builder
    .Services.AddHttpClient<IbkrDownloadService>(
        (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<IbkrApiSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", settings.Accept);
        }
    )
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<IbkrApiSettings>>().Value;
        if (settings.BypassSsl)
        {
            // Development only: bypass SSL validation
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
        }
        // Production: use default handler (no need to set anything)
        return new HttpClientHandler();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapGrpcService<JobGrpcService>();
app.MapGet("/", () => "Use /api/* for REST or gRPC endpoints for structured calls.");

app.Run();
