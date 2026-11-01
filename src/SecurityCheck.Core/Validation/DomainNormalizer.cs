namespace SecurityCheck.Core.Validation;

/// <summary>
/// Plockar ut domänen ur det användaren skriver, t.ex. en hel adress eller en e-postadress.
/// Nekar aldrig något, indata som inte går att tolka returneras trimmad, så att DomainValidator
/// nekar den med sitt vanliga meddelande. Måste köras före DomainValidator, aldrig efter.
/// </summary>
public static class DomainNormalizer
{
    public static string Normalize(string input)
    {
        var trimmed = input.Trim();

        // Utan schema tolkar Uri inte texten som en adress, så vi lägger till ett.
        // "info@minfrisor.se" blir då användarnamnet "info" på värden "minfrisor.se".
        var candidate = trimmed.Contains("://") ? trimmed : $"https://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return trimmed;
        }

        // Host är redan i gemener och saknar schema, port, sökväg och användarnamn.
        // En avslutande punkt ("minfrisor.se.") är giltig i DNS men ser konstig ut i rapporten.
        return uri.Host.TrimEnd('.');
    }
}
