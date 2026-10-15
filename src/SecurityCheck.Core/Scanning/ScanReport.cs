using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Scanning;

/// <summary>
/// Resultatet av en hel skanning. Skapas bara via fabriksmetoderna,
/// så att en nekad domän aldrig har resultat och en godkänd aldrig har ett nekandemeddelande (samma mönster som CheckResult, D-018).
/// </summary>
public sealed record ScanReport
{
    public string Domain { get; }
    public bool IsDomainAccepted { get; }

    /// <summary>
    /// Meddelande från DomainValidator som visas för användaren när domänen nekas (D-021). Null om domänen godkändes.
    /// </summary>
    public string? RejectionMessage { get; }

    public IReadOnlyList<CheckResult> Results { get; }

    private ScanReport(string domain, bool isDomainAccepted,
        string? rejectionMessage, IReadOnlyList<CheckResult> results)
    {
        Domain = domain;
        IsDomainAccepted = isDomainAccepted;
        RejectionMessage = rejectionMessage;
        Results = results;
    }

    // Domänen nekades, inga kontroller kördes
    public static ScanReport Rejected(string domain, string message) =>
        new(domain, false, message, []);

    // Domänen godkändes och alla kontroller kördes
    public static ScanReport Completed(string domain, IReadOnlyList<CheckResult> results) =>
        new(domain, true, null, results);
}
