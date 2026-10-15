namespace SecurityCheck.Core.Validation;

/// <summary>
/// Resultatet av en domänvalidering (D-021). Skapas bara via fabriksmetoderna,
/// så att en nekad domän alltid har ett meddelande (samma mönster som CheckResult, D-018).
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

    // Godkänd domän
    public static DomainValidationResult Valid() => new(true, null);

    // Nekad domän. Användaren ska alltid få en förklaring (D-021), därför krävs ett meddelande
    public static DomainValidationResult Invalid(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message);
    }
}
