using System.Net;
using System.Net.Sockets;

namespace SecurityCheck.Core.Validation;

/// <summary>
/// Slår upp IP-adresser med systemets DNS. Returnerar en tom lista när domänen inte finns,
/// så att DomainValidator kan neka den med ett vänligt meddelande.
/// </summary>
public sealed class DnsResolver : IDnsResolver
{
    public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct)
    {
        try
        {
            return await Dns.GetHostAddressesAsync(host, ct);
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.HostNotFound or SocketError.NoData)
        {
            // Domänen finns inte, eller saknar IP-adresser. Det är ett förväntat utfall, inget fel.
            // Andra fel, t.ex. att DNS-servern inte svarar, kastas vidare.
            return [];
        }
    }
}
