namespace SecurityCheck.Core.Checks;

/// <summary>
/// Slår upp TXT-poster i DNS. Ett eget interface så att EmailProtectionCheck kan testas utan nätverk
/// </summary>
public interface ITxtRecordResolver
{
    /// <summary>
    /// Hämtar alla TXT-poster för namnet, t.ex. <c>example.com</c> eller <c>_dmarc.example.com</c>
    /// </summary>
    /// <returns>
    /// En sträng per post, med postens delsträngar ihopslagna.
    /// En tom lista när namnet inte finns eller saknar TXT-poster.
    /// </returns>
    /// <exception cref="IOException">DNS-servern svarade inte eller svarade med ett fel.</exception>
    Task<IReadOnlyList<string>> GetTxtRecordsAsync(string name, CancellationToken ct);
}