namespace SecurityCheck.Core.Validation;

/// <summary>
/// Resultatet av en domänvalidering. Skapas bara via fabriksmetoderna,
/// så att en nekad domän alltid har ett meddelande (samma mönster som CheckResult).
/// </summary>
public sealed record DomainValidationResult
{
    public bool IsValid { get; }

    /// <summary>
    /// Meddelande på klarspråk som visas för användaren när domänen nekas. Null om domänen godkändes.
    /// </summary>
    public string? Message { get; }

    private DomainValidationResult(bool isValid, string? message)
    {
        IsValid = isValid;
        Message = message;
    }

    /// <summary>
    /// Godkänd domän.
    /// </summary>
    public static DomainValidationResult Valid() => new(true, null);

    /// <summary>
    /// Nekad domän. Användaren ska alltid få en förklaring, därför krävs ett meddelande.
    /// </summary>
    public static DomainValidationResult Invalid(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message);
    }
}
