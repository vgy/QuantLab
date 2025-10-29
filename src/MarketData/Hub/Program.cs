using Microsoft.Extensions.Options;
using QuantLab.MarketData.Hub.Grpc;
using QuantLab.MarketData.Hub.Models.Config;
using QuantLab.MarketData.Hub.Models.DTO.Responses;
using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Services.Interface;
using QuantLab.MarketData.Hub.Services.Interface.Storage;
using QuantLab.MarketData.Hub.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IbkrSettings>(builder.Configuration.GetSection("IbkrSettings"));
builder.Services.Configure<BackgroundWorkerOptions>(
    builder.Configuration.GetSection("BackgroundWorker")
);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ICsvFileService, CsvFileService>();
builder.Services.AddSingleton(typeof(IBackgroundJobQueue<>), typeof(BackgroundJobQueue<>));
builder.Services.AddHostedService<BackgroundWorkerService<ResponseData>>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IIbkrDataService, IbkrDataService>();

builder
    .Services.AddHttpClient<IbkrDataDownloader>(
        (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<IbkrSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", settings.Accept);
        }
    )
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<IbkrSettings>>().Value;
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
