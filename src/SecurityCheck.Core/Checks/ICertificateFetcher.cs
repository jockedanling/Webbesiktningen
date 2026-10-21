namespace SecurityCheck.Core.Checks;

/// <summary>
/// Hämtar en domäns TLS-certifikat. Ett eget interface så att TlsCertificateCheck kan testas utan nätverk.
/// </summary>
public interface ICertificateFetcher
{
    /// <summary>
    /// Ansluter till domänen på port 443 och returnerar certifikatet, även om det är ogiltigt.
    /// </summary>
    /// <exception cref="System.Net.Sockets.SocketException">Domänen svarar inte.</exception>
    /// <exception cref="IOException">Anslutningen bröts.</exception>
    /// <exception cref="System.Security.Authentication.AuthenticationException">
    /// Domänen svarar men kan inte upprätta en säker anslutning.
    /// </exception>
    Task<CertificateInfo> FetchAsync(string host, CancellationToken ct);
}
