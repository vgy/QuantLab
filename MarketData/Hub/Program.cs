using QuantLab.MarketData.Hub.Services;
using QuantLab.MarketData.Hub.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddHostedService<BackgroundWorkerService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();

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