using System.Net;

namespace SecurityCheck.Core.Validation;

/// <summary>
/// Slår upp en domäns IP-adresser. Ett eget interface så att DomainValidator kan testas utan nätverk.
/// </summary>
public interface IDnsResolver
{
    /// <summary>
    /// Returnerar domänens IP-adresser eller en tom lista om domänen inte finns.
    /// </summary>
    /// <exception cref="System.Net.Sockets.SocketException">DNS-servern svarar inte.</exception>
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct);
}
