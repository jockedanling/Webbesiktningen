using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Kontrollerar domänens e-postskydd: SPF- och DMARC-posterna i DNS. En av strategierna i Strategy-mönstret.
/// </summary>
public sealed class EmailProtectionCheck : ISecurityCheck
{
    private readonly ITxtRecordResolver _resolver;

    public EmailProtectionCheck(ITxtRecordResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;

    }

    public string Name => "E-postskydd";

    public Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}