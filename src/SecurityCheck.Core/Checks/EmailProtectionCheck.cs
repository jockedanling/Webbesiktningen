using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Kontrollerar domänens e-postskydd: SPF- och DMARC-posterna i DNS. En av strategierna i Strategy-mönstret.
/// </summary>
public sealed class EmailProtectionCheck : ISecurityCheck
{
    private readonly ITxtRecordResolver _resolver;

    public EmailProtectionCheck(ITxtRecordResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    public string Name => "E-postskydd";

    public async Task<CheckResult> RunAsync(string domain, CancellationToken ct)
    {
        IReadOnlyList<string> domainRecords;
        IReadOnlyList<string> dmarcRecords;

        try
        {
            domainRecords = await _resolver.GetTxtRecordsAsync(domain, ct);
            dmarcRecords = await _resolver.GetTxtRecordsAsync($"_dmarc.{domain}", ct);
        }
        catch (IOException)
        {
            // DNS-servern svarade inte: säger inget om e-postskyddet.
            return CheckResult.Failed(Name, FailureReason.Unreachable);
        }

        var spfFinding = EvaluateSpf(domainRecords);
        var dmarcFinding = EvaluateDmarc(dmarcRecords);
        return CheckResult.Completed(Name, [spfFinding, dmarcFinding]);
    }

    private static Finding EvaluateSpf(IReadOnlyList<string> records)
    {
        var spfRecords = records
            .Where(r => r.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (spfRecords.Count == 0)
        {
            return SpfMissing();
        }
        if (spfRecords.Count > 1)
        {
            return MultipleSpfRecords();
        }

        var lastTerm = spfRecords[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];
        if (lastTerm.Equals("all", StringComparison.OrdinalIgnoreCase)
            || lastTerm.Equals("+all", StringComparison.OrdinalIgnoreCase))
        {
            return SpfAllowsEveryone();
        }

        return SpfOk();
    }

    private static Finding EvaluateDmarc(IReadOnlyList<string> records)
    {
        var dmarcRecords = records
            .Where(r => r.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (dmarcRecords.Count == 0)
        {
            return DmarcMissing();
        }
        
        var policyTag = dmarcRecords[0]
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(tag => tag.StartsWith("p=", StringComparison.OrdinalIgnoreCase));

        if (policyTag is null || string.Equals(policyTag, "p=none", StringComparison.OrdinalIgnoreCase))
        {
            return DmarcMonitorOnly();
        }

        return DmarcOk();
    }

    private static Finding SpfMissing() => new(
        "SPF saknas",
        Severity.High,
        "Det finns ingen uppgift om vilka servrar som får skicka e-post från domänen. Bedragare kan då lättare skicka mejl som ser ut att komma från ditt företag.",
        "Be den som sköter din e-post eller ditt webbhotell att lägga till en SPF-post för domänen.");

    private static Finding MultipleSpfRecords() => new(
        "Flera SPF-poster",
        Severity.High,
        "När det finns flera SPF-poster gäller ingen av dem, så domänen har i praktiken inget SPF-skydd.",
        "Be den som sköter din e-post att slå ihop SPF-posterna till en enda post.");

    private static Finding SpfAllowsEveryone() => new(
        "SPF tillåter alla avsändare",
        Severity.High,
        "SPF-posten tillåter vilken server som helst att skicka e-post från din domän. Det gör det lätt för bedragare att skicka mejl som ser ut att komma från ditt företag.",
        "Be den som sköter din e-post att ändra slutet av SPF-posten till -all eller ~all, så att bara era egna servrar får skicka.");
    
    private static Finding SpfOk() => new(
        "SPF skyddar domänen",
        Severity.Info,
        "Domänen anger vilka servrar som får skicka e-post från den, vilket gör det svårare att förfalska mejl från ditt företag.",
        "Ingen åtgärd behövs.");

    
    private static Finding DmarcMissing() => new(
        "DMARC saknas",
        Severity.High,
        "Om någon skickar ett falskt mejl i ditt företagsnamn får mottagerens e-postserver inget besked om att stoppa det. Bluffmejl kan då hamna direkt i kundens inkorg.",
        "Be den som sköter din e-post eller ditt webbhotell att lägga till en DMARC-post för domänen.");
    
    private static Finding DmarcMonitorOnly() => new(
        "DMARC övervakar bara",
        Severity.Medium,
        "Domänen har DMARC, men den är inställd på att bara samla in rapporter. Förfalskade mejl släpps fortfarande igenom.",
        "Be den som sköter din e-posrt att ändra policyn till quarantine eller reject när ni har sett att er egen e-posrt kommer fram som den ska.");
    
    private static Finding DmarcOk() => new(
        "DMARC skyddar domänen",
     Severity.Info,
        "Mottagarens e-postservrar stoppar eller sorterar bort mejl som utger sig för att komma från din domän utan att göra det.",
        "Ingen åtgärd behövs.");
}