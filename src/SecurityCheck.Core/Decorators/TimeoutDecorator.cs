using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Decorators;

/// <summary>
/// Avbryter kontrollen när tidsgränsen löper ut och returnerar Failed(Timeout).
/// Avbrott från anroparen kastas vidare.
/// </summary>
public sealed class TimeoutDecorator : ISecurityCheck
{
    private readonly ISecurityCheck _inner;
    private readonly TimeSpan _timeout;

    public TimeoutDecorator(ISecurityCheck inner, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(inner);
        _timeout = timeout;
        _inner = inner;

    }

    public string Name => _inner.Name;

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        // Länkad så att både anroparens avbrott och tidsgränsen avbryter den inre kontrollen.
        // using släpper timern och registreringen på ct efter varje körning.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            return await _inner.RunAsync(domain, timeoutCts.Token).WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return CheckResult.Failed(Name, FailureReason.Timeout);
        }
    }
}