namespace SecurityCheck.Core.Validation;

/// <summary>
/// Kontrollerar att en domän är säker att skanna (SSRF-skydd) innan några kontroller körs (D-015).
/// </summary>
public interface IDomainValidator
{
    /// <summary>
    /// Validerar domänen. Asynkron eftersom valideringen slår upp domänens IP-adresser i DNS.
    /// </summary>
    Task<DomainValidationResult> ValidateAsync(string domain, CancellationToken ct);
}
