using DnsClient;

namespace SecurityCheck.Core.Checks;
/// <summary>
/// Slår upp TXT-poster med DnsClient. Den enda klassen i e-postkontrollen som pratar med nätverket.
/// </summary>
public sealed class TxtRecordResolver : ITxtRecordResolver
{
    private readonly LookupClient _client = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(3),
        Retries = 1
    });

    public async Task<IReadOnlyList<string>> GetTxtRecordsAsync(string name, CancellationToken ct)
    {
        IDnsQueryResponse response;
        try
        {
            response = await _client.QueryAsync(name, QueryType.TXT, QueryClass.IN, ct);
        }
        catch (DnsResponseException ex)
        {
            ct.ThrowIfCancellationRequested();
            throw new IOException($"DNS-uppslaget för {name} misslyckades.", ex);
        }

        if (response.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain)
        {
            return [];
        }

        if (response.HasError)
        {
            throw new IOException($"DNS-servern svarade med ett fel för {name}: {response.ErrorMessage}");
        }

        return response.Answers.TxtRecords()
            .Select(record => string.Concat(record.Text))
            .ToList();
    }
}