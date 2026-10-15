namespace SecurityCheck.Core.Contracts;

/// <summary>
/// En säkerhetskontroll som körs mot en domän. Strategy-gränssnittet:
/// varje kontroll är en utbytbar strategi som SecurityScanner kör utan att känna till detaljerna.
/// </summary>
public interface ISecurityCheck
{
    /// <summary>
    /// Namnet som visas i rapporten, t.ex. "TLS-certifikat".
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Kör kontrollen. Returnerar Completed med fynd (tom lista om inga problem hittades),
    /// eller Failed med en FailureReason om kontrollen inte kunde genomföras.
    /// </summary>
    Task<CheckResult> RunAsync(string domain, CancellationToken ct);
}
