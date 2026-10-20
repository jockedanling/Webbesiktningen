using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace SecurityCheck.Core.Validation;

/// <summary>
/// Kontrollerar att en domän är säker att skanna (SSRF-skydd, D-014, D-015).
/// Kontrollerar först formatet och slår sedan upp domänen i DNS. Alla adresser måste vara publika.
/// </summary>
public sealed class DomainValidator : IDomainValidator
{
    private readonly IDnsResolver _dns;

    public DomainValidator(IDnsResolver dns)
    {
        ArgumentNullException.ThrowIfNull(dns);
        _dns = dns;
    }

    public async Task<DomainValidationResult> ValidateAsync(string domain, CancellationToken ct)
    {
        var formatResult = ValidateFormat(domain);
        if (!formatResult.IsValid)
        {
            return formatResult;
        }

        // Slå upp ASCII-formen, så att t.ex. "åkeri.se" fungerar på alla plattformar
        var host = new IdnMapping().GetAscii(domain.Trim());
        var addresses = await _dns.ResolveAsync(host, ct);

        if (addresses.Length == 0)
        {
            return DomainValidationResult.Invalid("Vi hittade inte domänen. Kontrollera stavningen.");
        }

        // Alla adresser måste vara publika, annars kan en intern adress blandas in bland publika
        if (!addresses.All(IsPublic))
        {
            return DomainValidationResult.Invalid("Domänen är inte tillåten.");
        }

        return DomainValidationResult.Valid();
    }

    private static bool IsPublic(IPAddress address)
    {
        // "::ffff:127.0.0.1" är en IPv4-adress förklädd som IPv6
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0                                  // "den här datorn"
                || b[0] == 10                                   // privat
                || b[0] == 127                                  // loopback
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)   // CGNAT
                || (b[0] == 169 && b[1] == 254)                 // link-local, metadatatjänst
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // privat
                || (b[0] == 192 && b[1] == 168)                 // privat
                || b[0] >= 224);                                // multicast, reserverat, broadcast
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !(address.Equals(IPAddress.IPv6Any)
                || address.Equals(IPAddress.IPv6Loopback)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6UniqueLocal
                || address.IsIPv6Multicast);
        }

        // Okänd adresstyp: neka hellre än att släppa igenom
        return false;
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
