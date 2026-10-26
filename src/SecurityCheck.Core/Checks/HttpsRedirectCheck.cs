using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Kontrollerar att domänen skickar besökare från http:// vidare till https://. En av strategierna i Strategy-mönstret.
/// </summary>
public sealed class HttpsRedirectCheck : ISecurityCheck
{
    private readonly IRedirectFetcher _fetcher;

    public HttpsRedirectCheck(IRedirectFetcher fetcher)
    {
        ArgumentNullException.ThrowIfNull(fetcher);
        _fetcher = fetcher;
    }

    public string Name => "HTTPS-omdirigering";

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        RedirectResponse response;
        try
        {
            response = await _fetcher.FetchAsync(domain, ct);
        }
        catch (HttpRequestException)
        {
            // Sajten svarar inte på http://: säger inget om omdirigeringen
            return CheckResult.Failed(Name, FailureReason.Unreachable);
        }

        return CheckResult.Completed(Name, [Evaluate(response)]);
    }

    private static Finding Evaluate(RedirectResponse response)
    {
        if (!IsRedirect(response.StatusCode) || response.Location is null)
        {
            return NoRedirect();
        }

        if (response.Location.IsAbsoluteUri && response.Location.Scheme == Uri.UriSchemeHttps)
        {
            return RedirectsToHttps();
        }

        return RedirectsElsewhere();
    }

    private static bool IsRedirect(int statusCode) => statusCode is 301 or 302 or 303 or 307 or 308;

    private static Finding NoRedirect() => new(
        "Ingen omdirigering till HTTPS",
        Severity.High,
        "Besökare som skriver adressen utan https:// hamnar på en okrypterad sida. Det de skriver i ett kontaktformulär kan då läsas av andra på samma nätverk, som är ett öppet wifi.",
        "Be ditt webbhotell eller den som sköter webbplatsen att skicka alla besökare från http:// vidare till https://.");

    private static Finding RedirectsElsewhere() => new(
        "Skickar inte direkt vidare till HTTPS",
        Severity.Medium,
        "Webbplatsen skickar besökare vidare, men inte direkt till en säker https-adress. Det kan bli rätt till slut, men första steget sker okrypterat",
        "Be den som sköter webbplatsen att skicka besökare direkt från http:// till https://."
    );

    private static Finding RedirectsToHttps() => new(
        "Skickar vidare till HTTPS",
        Severity.Info,
        "Besökare som skriver adressen utan https:// skickas automatiskt till den säkra versionen av webbplatsen.",
        "Ingen åtgärd behövs."
    );

}
