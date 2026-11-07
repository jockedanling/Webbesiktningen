namespace SecurityCheck.Core.Contracts;

/// <summary>
/// Resultatet av en kontroll för en domän.
/// Skapas bara via <see cref="Completed"/> eller <see cref="Failed"/>, så att Status,
/// Findings och FailureReason alltid hänger ihop. 
/// </summary>
public sealed record CheckResult
{
    public string CheckName { get; }
    public CheckStatus Status { get; }
    public IReadOnlyList<Finding> Findings { get; }
    public FailureReason? FailureReason { get; }

    
    private CheckResult(string checkName, CheckStatus status,
    IReadOnlyList<Finding> findings, FailureReason? failureReason)
    {
        CheckName = checkName;
        Status = status;
        Findings = findings;
        FailureReason = failureReason;
    }
    // Lyckat resultat. En tom lista betyder att inget hittades.
    public static CheckResult Completed(string checkName, IReadOnlyList<Finding> findings) =>
    new(checkName, CheckStatus.Completed, findings, null);

    // Kontrollen kunde inte genomföras. Säger ingenting om domänen är säker.
    public static CheckResult Failed(string checkName, FailureReason reason) =>
    new(checkName, CheckStatus.Failed, [], reason);
}