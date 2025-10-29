namespace QuantLab.MarketData.Hub.Tests.Services;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using QuantLab.MarketData.Hub.Services;

[TestFixture]
public class MarketDataServiceTests
{
    private Mock<IHttpClientFactory> _httpFactoryMock = null!;
    private Mock<ILogger<MarketDataService>> _loggerMock = null!;
    private MarketDataService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _httpFactoryMock = new();
        _loggerMock = new();
    }

    [Test]
    public void Start_Should_Set_IsRunning_True()
    {
        _httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new MockHandler(HttpStatusCode.OK, "{}")));

        _service = new(_loggerMock.Object, _httpFactoryMock.Object);
        _service.Start();

        _service.Status.IsRunning.Should().BeTrue();
    }

    [Test]
    public void Stop_Should_CancelAndSetNotRunning()
    {
        _httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new MockHandler(HttpStatusCode.OK, "{}")));

        _service = new(_loggerMock.Object, _httpFactoryMock.Object);
        _service.Start();
        _service.Stop();

        _service.Status.IsRunning.Should().BeFalse();
    }

    [Test]
    public async Task Should_Stop_On_Unauthorized()
    {
        _httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new MockHandler(HttpStatusCode.Unauthorized, "Unauthorized")));

        _service = new(_loggerMock.Object, _httpFactoryMock.Object);
        _service.Start();

        await Task.Delay(250);

        _service.Status.IsRunning.Should().BeFalse();
        _service.Status.LastError.Should().Be("Unauthorized (401)");
    }

    [Test]
    public async Task Should_Record_SuccessfulFetch()
    {
        _httpFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new MockHandler(HttpStatusCode.OK, """{"symbol":"AAPL"}""")));

        _service = new(_loggerMock.Object, _httpFactoryMock.Object);
        _service.Start();

        await Task.Delay(250);

        _service.Status.FetchCount.Should().BeGreaterThan(0);
        _service.Status.LastResponseSnippet.Should().Contain("AAPL");
    }

    private sealed class MockHandler(HttpStatusCode status, string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage _,
            CancellationToken __
        ) =>
            Task.FromResult(
                new HttpResponseMessage(status) { Content = new StringContent(response) }
            );
    }
}
