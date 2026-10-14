# Projektkontext – Webbesiktningen

> **Produktnamn:** Webbesiktningen (syns i gränssnitt och README). Koden använder engelska namn (`SecurityCheck.*`).

> Den här filen beskriver vad projektet är, hur det är byggt och var vi befinner oss.
> Den är skriven för både människor och AI-assistenter. Ge den till din AI i början av varje session.
> Uppdatera avsnittet **Nuläge** löpande. Beslut och motiveringar finns i [`DECISIONS.md`](DECISIONS.md).

**Senast uppdaterad:** 2026-10-14

---

## 1. Vad vi bygger

En applikation där en småföretagare anger sin domän och får en begriplig säkerhetsrapport med konkreta åtgärder, skriven på klarspråk.

**Målgrupp:** icke-tekniska företagare (frisörer, hantverkare, små webbshoppar) som har webbplats och företagsmejl men ingen IT-avdelning.

**Vår skillnad mot befintliga verktyg** (Mozilla Observatory, SSL Labs, securityheaders.com): de är byggda för tekniker. Vi kombinerar flera kontroller i en rapport och förklarar varje fynd och åtgärd så att en icke-tekniker förstår.

**Sammanhang:** grupprojekt i kursen APR400 – Avancerad programmering med C#, Högskolan Väst, HT2026. Deadline **2026-11-08 23:59**. Betyg: godkänd/underkänd. Projektet ska också fungera som portföljprojekt.

---

## 2. Team och ansvar

| | Erik | Joakim |
|---|---|---|
| Design pattern | **Strategy** – `ISecurityCheck`, `SecurityScanner` | **Decorator** – `TimeoutDecorator`, `ErrorHandlingDecorator`, `LoggingDecorator` |
| Kontroller | TLS-certifikat, Säkerhetsheaders | E-postskydd (SPF/DMARC), HTTPS-omdirigering |
| Övrigt | `DomainValidator`, konsolappen | Sätter upp repot och CI, rate limiting |

Varje person implementerar sitt mönster själv tillsammans med AI och spelar in det live (individuell filmuppgift).

### Fördelning efter kontraktet

Kontraktet i `Contracts/` görs tillsammans. Därefter arbetar var och en på egen hand med sina delar, i ordningen nedan. Var och en skriver tester för sin egen kod i motsvarande mapp under `tests/SecurityCheck.Tests/`.

**Erik**

| # | Uppgift | Plats |
|---|---|---|
| 1 | `ISecurityCheck` (del av kontraktet, kräver att båda godkänner) | `Contracts/` |
| 2 | `DomainValidator` med eget resultat och meddelande till användaren (TDD) | `Validation/` |
| 3 | `SecurityScanner` – validerar först, kör sedan kontrollerna parallellt | `Scanning/` |
| 4 | `TlsCertificateCheck` (minimum) | `Checks/` |
| 5 | Tunn konsolapp som kör en skanning från början till slut | `SecurityCheck.Cli` |
| 6 | `SecurityHeadersCheck` (mål) | `Checks/` |

**Joakim**

| # | Uppgift | Plats |
|---|---|---|
| 1 | `ErrorHandlingDecorator` | `Decorators/` |
| 2 | `TimeoutDecorator` | `Decorators/` |
| 3 | `LoggingDecorator` | `Decorators/` |
| 4 | `EmailProtectionCheck` – SPF/DMARC (minimum) | `Checks/` |
| 5 | `HttpsRedirectCheck` (mål) | `Checks/` |
| 6 | Rate limiting i koden som startar en skanning (se avsnitt 8) | `SecurityCheck.Web` |

**Tillsammans:** kontraktet, att koppla ihop decorators runt kontrollerna, Blazor-gränssnittet (vecka 3–4), driftsättning till Azure, README och den skriftliga rapporten.

Ändra inte i den andras filer. Behövs en ändring där, ta det med ägaren eller föreslå den i en PR som ägaren granskar.

---

## 3. Funktionalitet

### Kontroller (alla passiva)

| Kontroll | Vad den kollar | Prioritet |
|---|---|---|
| TLS-certifikat | Giltighet och antal dagar kvar | **Minimum** |
| E-postskydd | SPF- och DMARC-poster i DNS | **Minimum** |
| Säkerhetsheaders | HSTS, CSP, X-Content-Type-Options m.fl. | Mål |
| HTTPS-omdirigering | Skickas `http://` vidare till `https://`? | Mål |

Alla kontroller tar en domän som indata, i enlighet med kontraktet.

### Utanför scope

- Lösenordskontroll: appen tar aldrig emot lösenord (se D-011)
- Aktiv skanning: inga portskanningar, inloggningsförsök eller sårbarhetstester
- Användarkonton, databas och historik
- Massskanning av många domäner
- PDF-export (möjligen senare)

---

## 4. Teknik

- **Språk/plattform:** C# på .NET 10 (LTS)
- **Tester:** xUnit + NSubstitute
- **Gränssnitt:** tunn konsolapp först, sedan Blazor
- **Versionshantering:** Git + GitHub, pull requests till `main`
- **CI:** GitHub Actions (bygg med `-warnaserror` + tester vid varje PR)
- **Hosting:** Azure App Service via högskolekonton. Koden ligger på GitHub och driftsätts automatiskt med GitHub Actions vid merge till `main`.

---

## 5. Arkitektur

```
SecurityCheck.slnx
├── src/
│   ├── SecurityCheck.Core/          ← all logik, inget UI
│   │   ├── Contracts/               ← GEMENSAMT: ändras bara om båda godkänner
│   │   ├── Validation/              ← DomainValidator (SSRF-skydd, körs först)
│   │   ├── Scanning/                ← Erik: SecurityScanner (kör kontroller parallellt)
│   │   ├── Checks/                  ← en klass per kontroll
│   │   └── Decorators/              ← Joakim
│   ├── SecurityCheck.Cli/           ← tunn konsolapp (inte ".Console" – krockar med System.Console)
│   └── SecurityCheck.Web/           ← Blazor (vecka 2–3)
├── tests/
│   └── SecurityCheck.Tests/
└── docs/
    ├── PROJECT_CONTEXT.md
    └── DECISIONS.md
```

**Flöde:** gränssnittet skickar en domän till `SecurityScanner` → scannern låter `DomainValidator` kontrollera att domänen är säker (annars avbryts skanningen med ett meddelande till användaren) → scannern kör alla kontroller parallellt (`Task.WhenAll`) → varje kontroll är omsluten av decorators (timeout, felhantering, loggning) → resultaten samlas i en rapport → gränssnittet visar den.

Exempel på hur en kontroll kopplas ihop:

```csharp
ISecurityCheck check = new LoggingDecorator(
    new ErrorHandlingDecorator(
        new TimeoutDecorator(new TlsCertificateCheck(), TimeSpan.FromSeconds(5))));
```

---

## 6. Kontraktet

Detta är det gemensamma interfacet som båda kodar mot. **Ändringar kräver att båda godkänner i en PR.**
`ISecurityCheck` skrivs av Erik som en del av Strategy-mönstret (se D-023); övriga typer finns i `Contracts/`.

```csharp
public interface ISecurityCheck
{
    string Name { get; }
    Task<CheckResult> RunAsync(string domain, CancellationToken ct);
}

public enum Severity { Info, Low, Medium, High }

public enum CheckStatus { Completed, Failed } // Failed = kontrollen kunde inte genomföras

public enum FailureReason
{
    Timeout,        // tog för lång tid
    Unreachable,    // sidan/DNS svarade inte
    InvalidInput,   // ogiltig domän
    Unexpected      // något vi inte förutsåg = troligen en bugg hos oss
}

public sealed record Finding(
    string Title,           // "DMARC saknas"
    Severity Severity,
    string Explanation,     // vad det betyder, på klarspråk
    string Recommendation); // vad företagaren ska göra

// Skapas bara via fabriksmetoderna, så att Status och FailureReason alltid stämmer överens (D-018)
public sealed record CheckResult
{
    public string CheckName { get; }
    public CheckStatus Status { get; }
    public IReadOnlyList<Finding> Findings { get; }
    public FailureReason? FailureReason { get; }

    private CheckResult(...) { ... }

    public static CheckResult Completed(string checkName, IReadOnlyList<Finding> findings); // FailureReason = null
    public static CheckResult Failed(string checkName, FailureReason reason);               // Findings = tom lista
}
```

Användning:

```csharp
return CheckResult.Completed(Name, findings);
return CheckResult.Failed(Name, FailureReason.Timeout);
```

### Felhantering

- **Kontrollen hittade ett problem** → `CheckResult.Completed(...)` med ett eller flera `Finding`.
- **Kontrollen hittade inga problem** → `CheckResult.Completed(...)` med en tom lista.
- **Kontrollen kunde inte genomföras** → `CheckResult.Failed(...)` med en `FailureReason`. Användaren ser ett vänligt meddelande, t.ex. "Kontrollen kunde inte genomföras just nu (webbplatsen svarade inte). Försök igen om en stund."
- `Completed` betyder att kontrollen gick att genomföra, **inte** att sidan är säker. Ett `Completed`-resultat kan innehålla fynd med `Severity.High`.
- `ErrorHandlingDecorator` fångar undantag och gör om dem till `CheckResult.Failed(...)` med `FailureReason.Unexpected`, så att enskilda kontroller inte behöver egen `try/catch`.
- **Tekniska detaljer (exception-meddelanden, stack traces) visas aldrig för användaren.** De loggas för utvecklarna.

---

## 7. Arbetssätt

- **Branches:** `feat/<namn>-<funktion>`, t.ex. `feat/erik-tls-check`. Aldrig direkt till `main`.
- **Pull requests:** den andra personen granskar alltid. Små PR:er, en funktion i taget.
- **Hämta `main` dagligen** till den egna branchen.
- **Definition of Done:**
  - [ ] Bygger utan varningar
  - [ ] Har tester som går igenom
  - [ ] Granskad av den andra
  - [ ] Resurser disposas och event handlers avregistreras
- **Kommunikation:** sms löpande, möten på Discord.
- **Beslut:** varje tekniskt beslut loggas i `DECISIONS.md` med vad, varför och var. Underlag till rapporten.
- **AI-chattar:** sparas löpande som underlag till filmerna.

---

## 8. Säkerhet

### Regler i koden

- Appen tar **aldrig** emot lösenord eller andra hemligheter.
- `LoggingDecorator` loggar inga känsliga detaljer.
- Bara passiva kontroller.
- Inga tekniska felmeddelanden till användaren (risk för informationsläckage).

### Skydd för vår egen app (se D-014)

Appen gör förfrågningar mot adresser som användaren anger, vilket gör den till en möjlig attackvektor. Följande ska implementeras:

| # | Skydd | Prioritet |
|---|---|---|
| 1 | **Domänvalidering / SSRF-skydd:** slå upp IP och neka privata, interna, lokala och link-local adresser (t.ex. `localhost`, `10.x`, `192.168.x`, `169.254.169.254`). Följ inte omdirigeringar automatiskt. | Från start |
| 2 | **Rate limiting:** begränsa antal skanningar per användare. Läggs i koden som startar en skanning (`System.Threading.RateLimiting`), eftersom Blazor Server-interaktioner går över WebSocket och inte fångas av HTTP-middleware. | Från start |
| 3 | **HTTPS och HSTS:** `UseHttpsRedirection`, `UseHsts`, "HTTPS Only" i App Service. | Löpande |
| 4 | **Inga detaljerade fel i produktion:** generell felhantering med vänliga meddelanden. | Löpande |
| 5 | **Inga hemligheter i repot:** driftsättningsnycklar i GitHub Secrets, konfiguration i App Service. | Löpande |
| 6 | **Säkerhetsheaders i vår egen app:** HSTS, CSP, X-Content-Type-Options m.fl. via middleware. Mål: appen ska få högsta betyg när den kontrollerar sig själv. | Löpande |
| 7 | **Begränsa utgående förfrågningar:** timeout (decorators), max svarsstorlek, max antal omdirigeringar. | Löpande |
| 8 | **Dependabot** aktiverat i GitHub för sårbara NuGet-paket. | Löpande |

Domänvalideringen ligger i en separat `DomainValidator` som `SecurityScanner` anropar innan några kontroller körs (se D-015). Den byggs med TDD.

---

## 9. Kurskrav och var de uppfylls

| Krav | Var |
|---|---|
| .NET med aktiv support | .NET 10 |
| Avancerade C#-koncept | Async (`Task.WhenAll`, `CancellationToken`), generics/collections, records |
| Unit tests | xUnit + NSubstitute; nätverk mockas, decorators testas mot fejkade kontroller |
| Ett design pattern per person | Strategy (Erik), Decorator (Joakim) |
| Mindful memory usage | Återanvänd `HttpClient`, `TcpClient`/`SslStream` disposas, event handlers avregistreras |
| Hög kodkvalitet | Logik separerad från UI, ett ansvar per klass, kodgranskning, Nakov kap. 21 |
| README | Bygga, köra, testa, samt etisk avgränsning |

Skriftlig rapport: 1500–2000 ord, APA7. Ska täcka projektbeskrivning, avancerade koncept, testmetod, minnesanvändning, design patterns samt förbättringar och framtida arbete.

---

## 10. Tidsplan

| Vecka | Fokus |
|---|---|
| 1 (9–18 okt) | Repo, solution, kontrakt, CI. Strategy + Decorator, 2 kontroller med tester. Spela in filmerna. |
| 2 (19–25 okt) | Resterande kontroller, parallell körning, timeout/cancellation |
| 3 (26 okt–1 nov) | Betyg och rapportvy, Blazor-gränssnitt, kodgranskning |
| 4 (2–8 nov) | README, skriftlig rapport, buffert |

**Steg 2 (om tid finns):** resultat live i Blazor (`Task.WhenEach`/`IAsyncEnumerable`), minnesmätning med BenchmarkDotNet, `RetryDecorator`/`CachingDecorator`, viktad poängsättning.

---

## 11. Nuläge

- [x] Idé vald och uppstartsmöte hållet (2026-10-13)
- [x] Grupp anmäld till läraren
- [x] Roller fördelade
- [x] Repo och solution-struktur uppsatt
- [ ] Kontraktet mergat till `main`
- [x] GitHub Actions uppsatt (bygg + test)
- [ ] Driftsättning till Azure App Service
- [ ] Tunn konsolapp som kör en kontroll från början till slut

---

## 12. Instruktioner till AI-assistenter

- Följ arkitekturen och kontraktet ovan. Föreslå inte ändringar i `Contracts/` utan att flagga det tydligt.
- Koden skrivs på engelska (namn) men kommentarer kan vara på svenska. Förklaringar till oss får vara på svenska.
- Förklara *varför* ett förslag passar, och nämn nackdelar eller alternativ. Vi ska kunna resonera kritiskt kring förslagen i våra filmer.
- Respektera säkerhetsreglerna i avsnitt 8.
- Håll förslag små och i linje med Definition of Done (inklusive tester).
