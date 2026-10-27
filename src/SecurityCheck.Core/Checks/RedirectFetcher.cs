namespace SecurityCheck.Core.Checks;

/// <summary>
/// Hämtar svaret på http://domän/ med HttpClient, utan att följa omdirigeringar (D-036).
/// Den enda klassen i HTTPS-kontrollen som pratar med nätverket.
/// </summary>
public sealed class RedirectFetcher : IRedirectFetcher
{
    private readonly HttpClient _client;

    public RedirectFetcher()
    {
        _client = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        });
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Webbesiktningen/1.0");
    }

    public async Task<RedirectResponse> FetchAsync(string domain, CancellationToken ct)
    {
        using var response = await _client.GetAsync($"http://{domain}/", HttpCompletionOption.ResponseHeadersRead, ct);
        return new RedirectResponse((int)response.StatusCode, response.Headers.Location);
    }
}
