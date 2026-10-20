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
    /// <exception cref="OperationCanceledException">
    /// Kastas bara när anroparens <paramref name="ct"/> är avbruten. En dekorerad kontroll kastar inga
    /// andra undantag; övriga fel blir Failed, även avbrott som inte kommer från anroparen (D-024, D-025).
    /// </exception>
    Task<CheckResult> RunAsync(string domain, CancellationToken ct);
}
