using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Decorators;

/// <summary>
/// Loggar när en kontroll startar, hur lång tid den tog och vad resultatet blev.
/// Resultatet skickas tillbaka oförändrat.
/// </summary>
public sealed partial class LoggingDecorator : ISecurityCheck
{
    private readonly ISecurityCheck _inner;
    private readonly ILogger<LoggingDecorator> _logger;

    public LoggingDecorator(ISecurityCheck inner, ILogger<LoggingDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner = inner;
        _logger = logger;
    }

    public string Name => _inner.Name;

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        LogCheckStarted(_logger, Name, domain);

        // GetTimestamp returnerar ett long, så inget Stopwatch-objekt skapas per körning
        long startTimestamp = Stopwatch.GetTimestamp();

        CheckResult result = await _inner.RunAsync(domain, ct);

        long elapsedMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        // Bara antalet fynd loggas, inte innehållet (PROJECT_CONTEXT §8)
        if (result.Status == CheckStatus.Completed)
        {
            LogCheckCompleted(_logger, Name, domain, result.Findings.Count, elapsedMs);
        }
        else
        {
            LogCheckFailed(_logger, Name, domain, result.FailureReason, elapsedMs);
        }

        return result;
    }

    // Källgenererade loggmetoder: kollar IsEnabled först och undviker object[] och boxning
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Kontrollen {CheckName} startade för {Domain}")]
    private static partial void LogCheckStarted(ILogger logger, string checkName, string domain);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Kontrollen {CheckName} slutfördes för {Domain} med {FindingCount} fynd på {ElapsedMs} ms")]
    private static partial void LogCheckCompleted(ILogger logger, string checkName, string domain, int findingCount, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Kontrollen {CheckName} misslyckades för {Domain} ({FailureReason}) efter {ElapsedMs} ms")]
    private static partial void LogCheckFailed(ILogger logger, string checkName, string domain, FailureReason? failureReason, long elapsedMs);
}
