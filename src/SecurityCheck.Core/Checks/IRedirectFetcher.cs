namespace SecurityCheck.Core.Checks;

/// <summary>
/// Hämtar svaret på http://domän/.
/// Ett eget interface så att HttpsRedirectCheck kan testas utan nätverk.
/// </summary>
public interface IRedirectFetcher
{
    /// <summary>
    /// Gör en förfrågan till <c>http://domän/</c> och returnerar det första svaret.
    /// Omdirigeringar följs aldrig (D-036).
    /// </summary>
    /// <exception cref="HttpRequestException">Domänen svarar inte på port 80.</exception>
    Task<RedirectResponse> FetchAsync(string domain, CancellationToken ct);
}
