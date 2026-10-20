using Microsoft.Extensions.Logging;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Decorators;
/// <summary>
/// Fångar oväntade undantag, loggar dem och returnerar Failed(Unexpected). Avbrott från anroparen kastas vidare.
/// Använder Decorator som Design Pattern för att kunna lägga till beteende på det gemensamma interfacet ISecurityCheck.
/// </summary>
public sealed partial class ErrorHandlingDecorator : ISecurityCheck
{
    private readonly ISecurityCheck _inner;
    private readonly ILogger<ErrorHandlingDecorator> _logger;

    public ErrorHandlingDecorator(ISecurityCheck inner, ILogger<ErrorHandlingDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        _inner = inner;
        _logger = logger;
    }

    public string Name => _inner.Name;

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        try
        {
            return await _inner.RunAsync(domain, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedException(_logger, ex, Name, domain);
            return CheckResult.Failed(Name, FailureReason.Unexpected);
        }

    }


    [LoggerMessage(Level = LogLevel.Error,
    Message = "Kontrollen {CheckName} kastade ett oväntat undantag för {Domain}")]
    private static partial void LogUnexpectedException(ILogger logger, Exception exception, string checkName, string domain);
}