using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Hämtar en domäns TLS-certifikat med TcpClient och SslStream. Certifikatet hämtas även om det är ogiltigt,
/// så att TlsCertificateCheck kan förklara vad som är fel.
/// </summary>
public sealed class CertificateFetcher : ICertificateFetcher
{
    private const int HttpsPort = 443;

    public async Task<CertificateInfo> FetchAsync(string host, CancellationToken ct)
    {
        // Båda disposas när metoden är klar, även om ett undantag kastas
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host, HttpsPort, ct);

        var errors = SslPolicyErrors.None;
        await using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false,
            (_, _, chain, sslPolicyErrors) =>
            {
                errors = IgnoreExpiry(sslPolicyErrors, chain);

                // Godkänn alltid, så att handskakningen fortsätter och vi kan läsa ett ogiltigt certifikat.
                // Säkert här eftersom vi aldrig skickar eller tar emot några data över anslutningen.
                return true;
            });

        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, ct);

        // RemoteCertificate ägs av SslStream och disposas tillsammans med den
        var certificate = ssl.RemoteCertificate as X509Certificate2
            ?? throw new IOException("Servern skickade inget certifikat.");

        return new CertificateInfo(
            new DateTimeOffset(certificate.NotBefore),
            new DateTimeOffset(certificate.NotAfter),
            errors);
    }

    private static SslPolicyErrors IgnoreExpiry(SslPolicyErrors errors, X509Chain? chain)
    {
        // Ett utgånget certifikat ger också ChainErrors. Datumet kontrolleras redan i TlsCertificateCheck,
        // så ChainErrors behålls bara när kedjan har andra fel, t.ex. att certifikatet är självsignerat.
        if (errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors)
            && chain is not null
            && chain.ChainStatus.All(s => s.Status == X509ChainStatusFlags.NotTimeValid))
        {
            return errors & ~SslPolicyErrors.RemoteCertificateChainErrors;
        }

        return errors;
    }
}