using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Kontrollerar domänens TLS-certifikat: att det är giltigt, betrorr, gäller rätt namn och hur länge det gäller.
/// En av strategierna i Strategy-mönstret.
/// </summary>
public sealed class TlsCertificateCheck : ISecurityCheck
{
    private readonly ICertificateFetcher _fetcher;
    private readonly TimeProvider _time;

    public TlsCertificateCheck(ICertificateFetcher fetcher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(fetcher);
        ArgumentNullException.ThrowIfNull(time);
        _fetcher = fetcher;
        _time = time;
    }

    public string Name => "TLS-certifikat";

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        CertificateInfo certificate;
        try
        {
            certificate = await _fetcher.FetchAsync(domain, ct);
        }
        catch (AuthenticationException)
        {
            // Sajten svarar men kan inte upprätta en säker anslutning: ett fynd (D-024)
            return CheckResult.Completed(Name, [HandshakeFailed()]);
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            // Sajten svarar inte: säger inget om certifikatet (D-024)
            return CheckResult.Failed(Name, FailureReason.Unreachable);
        }

        return CheckResult.Completed(Name, Evaluate(certificate, domain));
    }

    private List<Finding> Evaluate(CertificateInfo certificate, string domain)
    {
        var now = _time.GetUtcNow();
        var findings = new List<Finding>();

        if (certificate.NotAfter <= now)
        {
            findings.Add(Expired());
        }

        if (certificate.NotBefore > now)
        {
            findings.Add(NotYetValid(certificate.NotBefore));
        }

        if (certificate.Errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            findings.Add(NameMismatch(domain));
        }

        if (certificate.Errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            findings.Add(NotTrusted());
        }

        // Antal dagar kvar visas bara när inget annat är fel, annars blir rapporten motsägelsefull
        if (findings.Count == 0)
        {
            var daysLeft = (int)Math.Floor((certificate.NotAfter - now).TotalDays);
            findings.Add(DaysLeft(daysLeft));
        }

        return findings;
    }

    private static Finding DaysLeft(int daysLeft)
    {
        // Let's Encrypt förnyar vid 30 dagar kvar, så bara de sista två veckorna ger en varning
        if (daysLeft <= 14)
        {
            return new Finding(
                "Certifikatet går snart ut",
                daysLeft <= 7 ? Severity.High : Severity.Medium,
                $"Certifikatet slutar gälla om {daysLeft} dagar. Då varnar webbläsare besökarna att sidan inte är säker.",
                "Kontakta ditt webbhotell eller den som sköter webbplatsen och be dem förnya certifikatet.");
        }

        return new Finding(
            "Certifikatet är giltigt",
            Severity.Info,
            $"Certifikatet gäller i {daysLeft} dagar till och skyddar trafiken mellan besökarna och webbplatsen.",
            "Ingen åtgärd behövs. Kontrollera gärna att certifikatet förnyas automatiskt.");
    }

    private static Finding Expired() => new(
        "Certifikatet har gått ut",
        Severity.High,
        "Webbläsare varnar besökarna att sidan inte är säker, och många lämnar sidan direkt.",
        "Kontakta ditt webbhotell eller den som sköter webbplatsen och be dem förnya certifikatet så snart som möjligt.");

    private static Finding NotYetValid(DateTimeOffset notBefore) => new(
        "Certifikatet gäller inte än",
        Severity.High,
        $"Certifikatet börjar gälla först {notBefore:yyyy-MM-dd}. Tills dess varnar webbläsare besökarna.",
        "Kontakta ditt webbhotell och be dem kontrollera att rätt certifikat är installerat.");

    private static Finding NameMismatch(string domain) => new(
        "Certifikatet gäller en annan adress",
        Severity.High,
        $"Certifikatet är utfärdat för en annan adress än {domain}, så webbläsare varnar besökarna.",
        $"Be ditt webbhotell installera ett certifikat som gäller {domain}. Gäller det bara www-adressen behöver det även täcka adressen utan www.");


    private static Finding NotTrusted() => new(
        "Certifikatet är inte betrott",
        Severity.High,
        "Certifikatet är inte utfärdat av en betrodd utfärdare, till exempel för att det är självsignerat. Webbläsare varnar besökarna.",
        "Be ditt webbhotell installera ett certifikat från en betrodd utfärdare, t.ex. det kostnadsfria Let's Encrypt.");

    private static Finding HandshakeFailed() => new(
        "Ingen säker anslutning",
        Severity.High,
        "Webbplatsen svarar, men det gick inte att upprätta en säker anslutning. Besökarna kan få en varning eller inte nå sidan alls.",
        "Kontakta ditt webbhotell och be dem kontrollera webbplatsens HTTPS-inställningar.");
}