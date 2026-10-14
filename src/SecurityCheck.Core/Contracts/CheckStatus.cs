namespace SecurityCheck.Core.Contracts;

/// <summary>
/// En CheckStatus för att se om besiktning är gjord och inte om sidan är säker.
/// </summary>
public enum CheckStatus
{
    /// <summary>
    /// Genomförd besiktning. Om problem hittades, finns de i Findings.
    /// </summary>
    Completed,
    /// <summary>
    /// Ej genomförd besiktning. Se FailureReason.
    /// </summary>
    Failed
}