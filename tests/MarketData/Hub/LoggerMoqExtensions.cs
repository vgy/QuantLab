namespace QuantLab.MarketData.Hub.UnitTests;

using Microsoft.Extensions.Logging;
using Moq;

internal static class LoggerMoqExtensions
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> mockLogger,
        LogLevel expectedLogLevel,
        Times times
    )
    {
        mockLogger.Verify(
            x =>
                x.Log(
                    It.Is<LogLevel>(l => l == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception?>(), // ✅ nullable exception
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true) // ✅ nullable Exception?
                ),
            times
        );
    }

    public static void VerifyLogMessage<T>(
        this Mock<ILogger<T>> mockLogger,
        LogLevel expectedLevel,
        string expectedMessageSubstring,
        Times times
    )
    {
        mockLogger.Verify(
            x =>
                x.Log(
                    It.Is<LogLevel>(l => l == expectedLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessageSubstring)),
                    It.IsAny<Exception?>(), // ✅ nullable exception
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>() // ✅ nullable Exception?
                ),
            times
        );
    }

    public static void VerifyLog<T>(
        this Mock<ILogger<T>> logger,
        LogLevel expectedLevel,
        string containsMessage
    )
    {
        logger.Verify(
            x =>
                x.Log(
                    It.Is<LogLevel>(l => l == expectedLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }
}
