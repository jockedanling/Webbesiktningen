using System.Globalization;
using System.Net;

namespace SecurityCheck.Core.Validation;

/// <summary>
/// Kontrollerar att en domän är säker att skanna (SSRF-skydd, D-014, D-015).
/// Steg 1: kontrollerar bara formatet. Steg 2 lägger till DNS-uppslagning.
/// </summary>
public sealed class DomainValidator : IDomainValidator
{
    private readonly IDnsResolver _dns;

    public DomainValidator(IDnsResolver dns)
    {
        ArgumentNullException.ThrowIfNull(dns);
        _dns = dns;
    }

    public Task<DomainValidationResult> ValidateAsync(string domain, CancellationToken ct)
    {
        // Inget await än, därför Task.FromResult. Blir async i steg 2 när DNS slås upp.
        return Task.FromResult(ValidateFormat(domain));
    }

    private static DomainValidationResult ValidateFormat(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return DomainValidationResult.Invalid("Skriv in en domän, t.ex. example.com.");
        }

        var host = domain.Trim();

        // IP-adresser först, eftersom IPv6 innehåller ':' och annars skulle tolkas som en adress med port
        if (IPAddress.TryParse(host.Trim('[', ']'), out _))
        {
            return DomainValidationResult.Invalid("Skriv in en domän, t.ex. example.com, inte en IP-adress.");
        }

        if (host.Contains(':') || host.Contains('/'))
        {
            return DomainValidationResult.Invalid("Skriv bara domänen, t.ex. example.com, utan https:// eller sökväg.");
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return DomainValidationResult.Invalid("Domänen är inte tillåten.");
        }

        if (!IsValidHostName(host))
        {
            return DomainValidationResult.Invalid("Det här ser inte ut som en giltig domän. Kontrollera stavningen.");
        }

        return DomainValidationResult.Valid();
    }

    private static bool IsValidHostName(string host)
    {
        string ascii;
        try
        {
            // Gör om t.ex. "åkeri.se" till "xn--keri-loa.se", så att svenska tecken fungerar
            ascii = new IdnMapping().GetAscii(host);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (ascii.Length > 253)
        {
            return false;
        }

        var labels = ascii.Split('.');

        // Minst två delar, så att interna namn som "intranet" nekas
        if (labels.Length < 2)
        {
            return false;
        }

        foreach (var label in labels)
        {
            if (label.Length is 0 or > 63
                || label.StartsWith('-') || label.EndsWith('-')
                || !label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                return false;
            }
        }

        return true;
    }
}
