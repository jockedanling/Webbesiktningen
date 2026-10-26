using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using SecurityCheck.Core.Checks;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Decorators;
using SecurityCheck.Core.Scanning;
using SecurityCheck.Core.Validation;

// Tunn konsolapp som kör en skanning från början till slut. All logik ligger i Core.
Console.OutputEncoding = Encoding.UTF8;

// Domänen kan skickas som argument (dotnet run -- example.com), annars frågar appen
var domain = args.Length > 0 ? args[0] : Prompt();

// Bara varningar och fel visas, så att rapporten inte blir överflödig i loggrader
using var loggerFactory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole(options => options.SingleLine = true)
    .SetMinimumLevel(LogLevel.Warning));

// Decorators kopplas ihop här, så att SecurityScanner bara känner till ISecurityCheck
ISecurityCheck Decorate(ISecurityCheck check) =>
    new LoggingDecorator(
        new ErrorHandlingDecorator(
            new TimeoutDecorator(check, TimeSpan.FromSeconds(10)),
            loggerFactory.CreateLogger<ErrorHandlingDecorator>()),
        loggerFactory.CreateLogger<LoggingDecorator>());

// Strategy: en ny kontroll läggs till med en rad här, utan att scannern ändras
ISecurityCheck[] checks =
[
    Decorate(new TlsCertificateCheck(new CertificateFetcher(), TimeProvider.System)),
    Decorate(new EmailProtectionCheck(new TxtRecordResolver())),
];

var scanner = new SecurityScanner(checks, new DomainValidator(new DnsResolver()));

// Ctrl+C avbryter skanningen snyggt istället för att döda processen
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += OnCancelKeyPress;

try
{
    Console.WriteLine($"Besiktar {domain} ...");
    var report = await scanner.ScanAsync(domain, cts.Token);
    Print(report);
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Skanningen avbröts.");
}
catch (SocketException)
{
    // DNS-servern svarade inte när domänen validerades (öppen fråga i DECISIONS.md)
    Console.WriteLine("Kunde inte slå upp domänen just nu. Försök igen om en stund.");
}
finally
{
    // Avregistrera event handlern (Definition of Done)
    Console.CancelKeyPress -= OnCancelKeyPress;
}

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true; // låt appen avsluta själv istället för att processen dödas direkt
    cts.Cancel();
}

static string Prompt()
{
    Console.Write("Vilken domän vill du besikta? ");
    return Console.ReadLine() ?? "";
}

static void Print(ScanReport report)
{
    if (!report.IsDomainAccepted)
    {
        Console.WriteLine(report.RejectionMessage);
        return;
    }

    foreach (var result in report.Results)
    {
        Console.WriteLine();
        Console.WriteLine($"== {result.CheckName} ==");

        if (result.Status == CheckStatus.Failed)
        {
            Console.WriteLine($"Kontrollen kunde inte genomföras just nu ({Describe(result.FailureReason)}). Försök igen om en stund.");
            continue;
        }

        foreach (var finding in result.Findings)
        {
            Console.WriteLine($"[{Label(finding.Severity)}] {finding.Title}");
            Console.WriteLine($" {finding.Explanation}");
            Console.WriteLine($" Åtgärd: {finding.Recommendation}");
        }
    }
}

// Tekniska detaljer visas aldrig för användaren, bara en vänlig förklaring
static string Describe(FailureReason? reason) => reason switch
{
    FailureReason.Timeout => "det tog för lång tid",
    FailureReason.Unreachable => "webbplatsen svarade inte",
    FailureReason.InvalidInput => "domänen gick inte att kontrollera",
    _ => "ett oväntat fel uppstod",
};

static string Label(Severity severity) => severity switch
{
    Severity.High => "Allvarligt",
    Severity.Medium => "Medel",
    Severity.Low => "Låg",
    _ => "Info",
};