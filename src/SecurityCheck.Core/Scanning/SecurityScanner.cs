using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Validation;

namespace SecurityCheck.Core.Scanning;

/// <summary>
/// Kör en samling säkerhetskontroller mot en domän. Context i Strategy-mönstret:
/// scannern känner bara till ISecurityCheck, inte vilka kontroller eller decorators som finns.
/// Normaliserar och validerar domänen först och kör sedan alla kontroller parallellt.
/// </summary>
public sealed class SecurityScanner
{
    private readonly IReadOnlyList<ISecurityCheck> _checks;
    private readonly IDomainValidator _validator;

    public SecurityScanner(IEnumerable<ISecurityCheck> checks, IDomainValidator validator)
    {
        ArgumentNullException.ThrowIfNull(checks);
        ArgumentNullException.ThrowIfNull(validator);

        // Kopiera listan så att den inte kan ändras utifrån efter att scannern skapats
        _checks = checks.ToList();
        _validator = validator;
    }

    /// <summary>
    /// Besiktar en domän. Returnerar en nekad rapport om domänen inte får skannas,
    /// annars en rapport med ett resultat per kontroll.
    /// </summary>
    /// <param name="domain">Det användaren skrev, t.ex. en domän, en hel adress eller en e-postadress.</param>
    /// <param name="ct">Avbryter skanningen. Då kastas OperationCanceledException.</param>
    /// <exception cref="System.Net.Sockets.SocketException">DNS-servern svarar inte när domänen valideras.</exception>
    public async Task<ScanReport> ScanAsync(string domain, CancellationToken ct = default)
    {
        var normalized = DomainNormalizer.Normalize(domain);

        var validation = await _validator.ValidateAsync(normalized, ct);
        if (!validation.IsValid)
        {
            // Message är aldrig null här, eftersom DomainValidationResult.Invalid kräver ett meddelande
            return ScanReport.Rejected(normalized, validation.Message!);
        }

        // Starta alla kontroller direkt och vänta på att alla blir klara.
        // Undantag fångas av ErrorHandlingDecorator runt varje kontroll, inte här.
        // Avbryter anroparen kastas OperationCanceledException vidare till anroparen.
        var tasks = _checks.Select(check => check.RunAsync(normalized, ct));
        CheckResult[] results = await Task.WhenAll(tasks);

        return ScanReport.Completed(normalized, results);
    }
}
