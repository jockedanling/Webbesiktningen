# Webbesiktningen


Webbesiktningen gör en snabb besiktning av ett företags webbplats och e-post, ungefär som en bilbesiktning. Man skriver in sin webbadress och får en rapport på vanlig svenska: vad som är bra, vad som behöver åtgärdas och vad man ska be sin webbyrå eller IT-person om.

Appen riktar sig till småföretagare som har en webbplats och företagsmejl men ingen IT-avdelning. De verktyg som finns, till exempel SSL Labs och Mozilla Observatory, är byggda för tekniker. Webbesiktningen samlar några av de viktigaste kontrollerna i en rapport och förklarar varje anmärkning utan tekniska felkoder.

Projektet är ett grupparbete i kursen APR400 – Avancerad programmering med C# vid Högskolan Väst, hösten 2026.

---

## Vad appen kontrollerar

Alla kontroller är **passiva**. Appen läser bara information som webbplatsen redan visar för alla besökare.

| Kontroll | Vad den tittar på | Exempel på anmärkning |
|---|---|---|
| **TLS-certifikat** | Att certifikatet är giltigt, gäller för rätt adress, är utfärdat av någon som webbläsare litar på och hur många dagar det har kvar | *Allvarligt* om certifikatet går ut inom 7 dagar, *Medel* inom 14 dagar |
| **E-postskydd** | Att domänen har en SPF-post, som anger vilka servrar som får skicka e-post, och en DMARC-post, som anger vad mottagaren ska göra med falska mail | *Medel* om DMARC bara övervakar (`p=none`) |
| **HTTPS-omdirigering** | Att `http://` skickar besökaren vidare till den krypterade `https://`-versionen | *Allvarligt* om sidan visas okrypterad |

Varje anmärkning har en nivå (*Allvarligt*, *Medel*, *Låg* eller *Info*), en förklaring och ett förslag på åtgärd. Om en kontroll inte kan genomföras, till exempel för att webbplatsen svarar för långsamt, står det i rapporten. De andra kontrollerna visas ändå.

---

## Så fungerar det

```
 Användaren skriver "https://www.minfrisor.se/kontakt"
              │
              ▼
 DomainNormalizer    plockar ut domänen: www.minfrisor.se
              │
              ▼
 DomainValidator     nekar interna adresser, IP-adresser och påhittade domäner (SSRF-skydd)
              │
              ▼
 SecurityScanner     kör alla kontroller parallellt (Strategy)
              │
     ┌────────┼────────┐
     ▼        ▼        ▼
  Logging(ErrorHandling(Timeout(kontroll)))   varje kontroll är inslagen i decorators
     │        │        │
     └────────┼────────┘
              ▼
 ScanReport  →  konsolappen eller webbgränssnittet
```

All logik ligger i `SecurityCheck.Core`. Konsolappen och webbgränssnittet kopplar bara ihop delarna och visar resultatet.

### Design patterns

- **Strategy (Erik):** varje kontroll implementerar interfacet `ISecurityCheck`. `SecurityScanner` känner bara till interfacet, så en ny kontroll läggs till med en rad där appen startar, utan att scannern ändras.
- **Decorator (Joakim):** tre decorators läggs runt varje kontroll och implementerar själva `ISecurityCheck`.
  - `TimeoutDecorator` avbryter en kontroll som tar längre än 10 sekunder.
  - `ErrorHandlingDecorator` fångar oväntade fel, så att en trasig kontroll inte stoppar hela besiktningen.
  - `LoggingDecorator` loggar hur lång tid varje kontroll tar och hur den gick.

  Kontrollerna själva vet ingenting om timeout, felhantering eller loggning.

---

## Ansvarsfördelning

| | Erik | Joakim |
|---|---|---|
| **Design pattern** | Strategy: `ISecurityCheck`, `SecurityScanner` | Decorator: `TimeoutDecorator`, `ErrorHandlingDecorator`, `LoggingDecorator` |
| **Kontroller** | `TlsCertificateCheck` med `CertificateFetcher` | `EmailProtectionCheck` med `TxtRecordResolver`, `HttpsRedirectCheck` med `RedirectFetcher` |
| **Validering** | `DomainValidator` och `DnsResolver` (SSRF-skydd) | `DomainNormalizer` |
| **Gränssnitt** | Konsolappen | Webbgränssnittet i Blazor |
| **Övrigt** | | Repo, CI med GitHub Actions och Dependabot |

**Tillsammans:** kontraktet i `Contracts/` (`CheckResult`, `Finding`, `Severity` med flera), sidans layout och innehåll, kodgranskning av varandras pull requests samt beslutsloggen.

Var och en har skrivit tester för sin egen kod.

---

## Teknik och bibliotek

**Plattform:** C# och .NET 10. Webbgränssnittet är byggt med Blazor.

### NuGet-paket

| Paket | Används till |
|---|---|
| [DnsClient](https://dnsclient.michaco.net/) | Slå upp TXT-poster i DNS, där SPF och DMARC finns. .NET har inget inbyggt stöd för TXT-uppslag |
| Microsoft.Extensions.Logging | Loggning i decorators och webbgränssnittet. Konsolappen loggar till konsolen från nivån *Warning* |
| xUnit | Testramverk |
| NSubstitute | Fejkade beroenden i testerna, så att kontrollerna kan testas utan nätverk |
| Microsoft.Extensions.Diagnostics.Testing | `FakeLogger`, för att testa vad decorators loggar |

### Inbyggt i .NET

| API | Används till |
|---|---|
| `TcpClient` och `SslStream` | Ansluta till webbplatsen och hämta certifikatet under TLS-handskakningen |
| `X509Certificate2`, `X509Chain`, `SslPolicyErrors` | Kontrollera certifikatets giltighetstid, namn och utfärdare |
| `HttpClient` med `SocketsHttpHandler` | Kontrollera omdirigeringen. Klienten återanvänds, följer aldrig omdirigeringar automatiskt och läser bara svarets headers |
| `Dns.GetHostAddressesAsync` och `IPAddress` | Slå upp domänens IP-adresser och neka privata, lokala och link-local adresser |
| `IdnMapping` | Hantera domännamn med å, ä och ö |
| `Task.WhenAll` | Köra kontrollerna parallellt |
| `CancellationToken` och `CancellationTokenSource.CancelAfter` | Timeout och avbrott, till exempel Ctrl+C i konsolappen |
| `TimeProvider` | Göra tiden utbytbar, så att testerna kan simulera ett certifikat som går ut om 7 dagar |
| `[LoggerMessage]` | Loggmeddelanden som genereras vid kompileringen, utan onödiga allokeringar |
| `record` | Oföränderliga dataobjekt i kontraktet |

---

## Kom igång

### Krav

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Internetanslutning, eftersom kontrollerna gör riktiga uppslag mot domänen

### Hämta och bygg

```bash
git clone https://github.com/jockedanling/Webbesiktningen.git
cd Webbesiktningen
dotnet build SecurityCheck.slnx
```

### Starta konsolappen

Skicka med domänen som argument:

```bash
dotnet run --project src/SecurityCheck.Cli -- minfrisor.se
```

Eller starta utan argument, så frågar appen efter domänen:

```bash
dotnet run --project src/SecurityCheck.Cli
```

Det går också att skriva en hel adress (`https://www.minfrisor.se/kontakt`) eller en e-postadress (`info@minfrisor.se`). Appen plockar då ut domänen själv. Tryck **Ctrl+C** för att avbryta en pågående besiktning.

### Starta webbgränssnittet

```bash
dotnet run --project src/SecurityCheck.Web
```

Under utveckling kan `dotnet watch --project src/SecurityCheck.Web` användas. Då byggs appen om automatiskt när en fil ändras.

### Kör testerna

```bash
dotnet test SecurityCheck.slnx
```

De flesta tester fejkar nätverket och fungerar utan internet. Några integrationstester, till exempel `TxtRecordResolverTests`, gör riktiga DNS-uppslag och kräver internetanslutning.

GitHub Actions bygger med `-warnaserror` och kör alla tester vid varje pull request till `main`.

---

## Projektstruktur

```
src/
  SecurityCheck.Core/     all logik: kontroller, decorators, validering och scanner
    Checks/               de tre kontrollerna och deras nätverksklasser
    Contracts/            kontraktet som alla delar använder
    Decorators/           timeout, felhantering och loggning
    Scanning/             SecurityScanner och ScanReport
    Validation/           normalisering och SSRF-skydd
  SecurityCheck.Cli/      konsolappen
  SecurityCheck.Web/      webbgränssnittet i Blazor
tests/
  SecurityCheck.Tests/    enhets- och integrationstester, i samma mappstruktur som Core
```

---

## Etisk avgränsning och säkerhet

Appen gör förfrågningar mot adresser som användaren skriver in. Den skulle därför kunna missbrukas om den inte var skyddad. Vi har valt att:

- **bara göra passiva kontroller.** Appen läser samma information som en vanlig besökare ser. Den försöker aldrig logga in, ta sig in, skanna portar eller belasta webbplatsen.
- **aldrig ta emot lösenord** eller andra hemligheter.
- **bara kontrollera riktiga, publika domäner.** `DomainValidator` slår upp domänens IP-adresser och nekar interna och lokala adresser, till exempel `localhost`, `192.168.x.x` och `169.254.169.254`. Det hindrar att appen används för att nå något som inte är tänkt att synas utåt (SSRF).
- **inte följa omdirigeringar.** En omdirigering kan peka på en intern adress som valideringen aldrig har kontrollerat.
- **begränsa varje förfrågan** med en timeout på 10 sekunder och genom att bara läsa svarens headers.
- **aldrig visa tekniska felmeddelanden** för användaren och inte logga känsliga uppgifter.

Använd appen för att kontrollera din egen webbplats eller en webbplats du har tillstånd att kontrollera.

**Resultatet är en hälsokontroll, inte en garanti.** Appen kontrollerar tre viktiga grundinställningar. Säkerhet handlar om mycket mer, till exempel lösenord, uppdateringar och säkerhetskopior.

---

## Begränsningar och framtida arbete

- **Appen körs lokalt och är inte driftsatt.** Om den publiceras behövs **rate limiting**, så att ingen kan använda servern för att skanna många domäner. I Blazor Server måste begränsningen ligga i koden som startar skanningen, eftersom klicken skickas över en WebSocket som vanligt HTTP-middleware inte ser.
- **E-postkontrollen och subdomäner:** `www.` tas bort innan SPF och DMARC slås upp, men andra subdomäner, till exempel `shop.minfrisor.se`, kan ge felaktiga anmärkningar.
- **Omdirigeringskedjor:** HTTPS-kontrollen bedömer bara det första svaret. En kedja som går via `http://www.` innan den hamnar på HTTPS bedöms som *Medel*.
- **Fler kontroller:** en kontroll av säkerhetsheaders (HSTS, CSP med flera) var planerad men hanns inte med.
- **Resultat live:** visa varje kontroll i webbgränssnittet så fort den är klar, i stället för att vänta på alla.

---
