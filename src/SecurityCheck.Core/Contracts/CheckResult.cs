namespace SecurityCheck.Core.Contracts;

public sealed record CheckResult
{
    public string CheckName { get; }
    public CheckStatus Status { get; }
    public IReadOnlyList<Finding> Findings { get; }
    public FailureReason? FailureReason { get; }

    // Privat konstruktor
    private CheckResult(string checkName, CheckStatus status,
    IReadOnlyList<Finding> findings, FailureReason? failureReason)
    {
        CheckName = checkName;
        Status = status;
        Findings = findings;
        FailureReason = failureReason;
    }
    // Lyckat resultat
    public static CheckResult Completed(string checkName, IReadOnlyList<Finding> findings) =>
    new(checkName, CheckStatus.Completed, findings, null);

    // Misslyckat resultat
    public static CheckResult Failed(string checkName, FailureReason reason) =>
    new(checkName, CheckStatus.Failed, [], reason);
}