using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Validation;

namespace SecurityCheck.Core.Scanning;

/// <summary>
/// Kör en samling säkerhetskontroller mot en domän. Context i Strategy-mönstret:
/// scannern känner bara till ISecurityCheck, inte vilka kontroller eller decorators som finns.
/// Validerar domänen först (D-015) och kör sedan alla kontroller parallellt.
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

    public async Task<ScanReport> ScanAsync(string domain, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(domain, ct);
        if (!validation.IsValid)
        {
            // Message är aldrig null här, eftersom DomainValidationResult.Invalid kräver ett meddelande
            return ScanReport.Rejected(domain, validation.Message!);
        }

        // Starta alla kontroller direkt och vänta på att alla blir klara.
        // Undantag fångas av ErrorHandlingDecorator runt varje kontroll, inte här.
        // Avbryter anroparen kastas OperationCanceledException vidare till anroparen (D-025).
        var tasks = _checks.Select(check => check.RunAsync(domain, ct));
        CheckResult[] results = await Task.WhenAll(tasks);

        return ScanReport.Completed(domain, results);
    }
}
