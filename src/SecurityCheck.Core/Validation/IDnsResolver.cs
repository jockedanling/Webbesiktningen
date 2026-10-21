using System.Net;

namespace SecurityCheck.Core.Validation;

/// <summary>
/// Slår upp en domäns IP-adresser. Ett eget interface så att DomainValidator kan testas utan nätverk.
/// </summary>
public interface IDnsResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct);
}
