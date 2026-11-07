namespace SecurityCheck.Core.Contracts;

/// <summary>
/// Om besiktningen misslyckades, visas bara vid Status = Failed
/// </summary>
public enum FailureReason
{
    /// <summary>
    /// Tog för lång tid
    /// </summary>
    Timeout,
    /// <summary>
    /// Sajten eller DNS svarade inte
    /// </summary>
    Unreachable,
    /// <summary>
    /// Domänen är ogiltig
    /// </summary>
    InvalidInput,
    /// <summary>
    /// Något oväntat, t.ex. bugg på vår sida
    /// </summary>
    Unexpected
}