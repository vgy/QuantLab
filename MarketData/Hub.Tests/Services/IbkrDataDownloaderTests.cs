namespace QuantLab.MarketData.Hub.Tests.Services;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using QuantLab.MarketData.Hub.Services;

[TestFixture]
public class IbkrDataDownloaderTests
{
    private Mock<ILogger<IbkrDataDownloader>> _loggerMock = null!;
    private HttpClient _httpClient = null!;
    private MockHttpMessageHandler _httpHandler = null!;
    private IbkrDataDownloader _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<IbkrDataDownloader>>();
        _httpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _sut = new IbkrDataDownloader(_httpClient, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _httpHandler.Dispose();
    }

    [Test]
    public async Task DownloadRecordAsync_ValidResponse_ReturnsParsedData()
    {
        // Arrange
        var symbol = "AAPL";
        var path = "https://api.test/symbols/AAPL";
        var responseData = new Dictionary<string, object> { ["AAPL"] = 12345 };

        _httpHandler.QueueResponse(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseData),
            }
        );

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Symbol.Should().Be(symbol);
        result.Data.Should().ContainKey("AAPL");
        ((JsonElement)result.Data["AAPL"]).GetInt32().Should().Be(12345);

        _loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce());
    }

    [Test]
    public async Task DownloadRecordAsync_RetriableStatusThenSuccess_RetriesAndSucceeds()
    {
        // Arrange
        var symbol = "MSFT";
        var path = "https://api.test/symbols/MSFT";
        _httpHandler.QueueResponse(new HttpResponseMessage((HttpStatusCode)429)); // Retry 1
        _httpHandler.QueueResponse(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new Dictionary<string, object> { ["MSFT"] = 42 }),
            }
        );

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Data.Should().ContainKey("MSFT");
        ((JsonElement)result.Data["MSFT"]).GetInt32().Should().Be(42);

        _loggerMock.VerifyLog(LogLevel.Warning, Times.AtLeastOnce());
        _loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce());
    }

    [Test]
    public async Task DownloadRecordAsync_AllRetriesFail_ReturnsEmptyData()
    {
        // Arrange
        var symbol = "TSLA";
        var path = "https://api.test/symbols/TSLA";
        _httpHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        _httpHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        _httpHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Symbol.Should().Be(symbol);
        result.Data.Should().BeEmpty();

        _loggerMock.VerifyLog(LogLevel.Warning, Times.AtLeast(2));
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public async Task DownloadRecordAsync_NonRetriableStatus_ReturnsEmptyData()
    {
        // Arrange
        var symbol = "NFLX";
        var path = "https://api.test/symbols/NFLX";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid request"),
        };
        _httpHandler.QueueResponse(response);

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Data.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public async Task DownloadRecordAsync_HttpClientThrowsException_ReturnsEmptyData()
    {
        // Arrange
        var symbol = "GOOG";
        var path = "https://api.test/symbols/GOOG";
        _httpHandler.ThrowOnSend(new HttpRequestException("Network failure"));

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Data.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    [Test]
    public void DownloadRecordAsync_CancelledBeforeRequest_ThrowsOperationCanceledException()
    {
        // Arrange
        var symbol = "META";
        var path = "https://api.test/symbols/META";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act + Assert
        Assert.CatchAsync<OperationCanceledException>(async () =>
            await _sut.DownloadRecordAsync(symbol, path, cts.Token)
        );
        _loggerMock.VerifyLogMessage(
            LogLevel.Warning,
            "Download cancelled for META",
            Times.AtLeastOnce()
        );
    }

    [Test]
    public async Task DownloadRecordAsync_InvalidJson_ReturnsEmptyData()
    {
        // Arrange
        var symbol = "IBM";
        var path = "https://api.test/symbols/IBM";
        _httpHandler.QueueResponse(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") }
        );

        // Act
        var result = await _sut.DownloadRecordAsync(symbol, path);

        // Assert
        result.Data.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce());
    }

    // -------------------------------
    // HELPER MOCK HANDLER
    // -------------------------------
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        private Exception? _exceptionToThrow;

        public void QueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

        public void ThrowOnSend(Exception ex) => _exceptionToThrow = ex;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (_exceptionToThrow != null)
                throw _exceptionToThrow;

            if (_responses.Count == 0)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
