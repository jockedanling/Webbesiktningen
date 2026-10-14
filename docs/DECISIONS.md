# Beslutslogg

> Varje tekniskt beslut loggas här med **vad**, **varför** och **var** i koden.
> Loggen är underlag till den skriftliga rapporten och till AI-assistenter (se [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md)).
> Nya beslut läggs till längst ner med nästa nummer. Ändra inte gamla beslut – skriv ett nytt som ersätter det.

**Mall:**

```
## D-XXX: Kort rubrik
**Datum:** ÅÅÅÅ-MM-DD · **Beslutat av:** namn
**Vad:** vad vi bestämde
**Varför:** motivering, och vilka alternativ vi valde bort
**Var:** vilka filer/klasser det påverkar
```

---

## D-001: Projektidé – Säkerhetskoll för småföretag
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** En app som gör passiva säkerhetskontroller av en domän och presenterar resultatet på klarspråk för icke-tekniska företagare.
**Varför:** Löser ett verkligt problem och ger naturliga tillfällen att använda async, design patterns och medveten minneshantering. Valdes framför en logganalysator, där det redan finns etablerade verktyg.
**Var:** Hela projektet.

## D-002: Roller och mönster
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Erik implementerar Strategy (`ISecurityCheck`, `SecurityScanner`) samt TLS- och header-kontrollerna. Joakim implementerar Decorator (timeout, felhantering, loggning) samt lösenords- och e-postkontrollerna.
**Varför:** Ett mönster per person enligt kurskraven. Mönstren samverkar via samma interface och kan byggas parallellt. Kontrollerna är fördelade så att var och en får en lättare och en svårare.
**Var:** `Scanning/`, `Decorators/`, `Checks/`.

## D-003: .NET 10
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Projektet byggs på .NET 10 (LTS).
**Varför:** Kursen kräver en version med aktiv support. .NET 8 tappar supporten i november 2026, ungefär vid deadline.
**Var:** Alla `.csproj`, `ci.yml`.

## D-004: Konsol först, sedan Blazor
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** En tunn konsolapp byggs först för att testa hela flödet. Blazor-gränssnitt byggs från vecka 2.
**Varför:** Snabb verifiering från början till slut, och den håller logiken i `Core` istället för i UI-koden.
**Var:** `SecurityCheck.Cli` (heter inte `.Console` eftersom det krockar med `System.Console`), `SecurityCheck.Web`.

## D-005: Kontrakt med records och en lista med fynd
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** `CheckResult` och `Finding` är immutabla `record`-typer. Ett resultat innehåller `IReadOnlyList<Finding>`. Ändringar i `Contracts/` kräver att båda godkänner.
**Varför:** En kontroll kan hitta flera problem (t.ex. flera saknade headers). Immutabilitet förhindrar att resultat ändras efter att de skapats. Ett fast kontrakt gör att vi kan jobba parallellt utan krockar.
**Var:** `Contracts/`.

## D-006: Felhantering med FailureReason
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Vi skiljer på "kontrollen hittade ett problem" (`Completed` + fynd) och "kontrollen kunde inte genomföras" (`Failed` + `FailureReason`). En `ErrorHandlingDecorator` gör om undantag till `Failed`-resultat. Användaren ser vänliga meddelanden; tekniska detaljer loggas men visas aldrig.
**Varför:** En timeout betyder inte att en sida är osäker. Att visa exception-meddelanden i en säkerhetsapp riskerar informationsläckage. En enum är tydligare och mer typsäker än numeriska felkoder.
**Var:** `Contracts/FailureReason.cs`, `Decorators/ErrorHandlingDecorator.cs`, gränssnittet.

## D-007: Bara passiva kontroller, inga sparade lösenord
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Ingen portskanning eller aktiva tester. Lösenord sparas och loggas aldrig; bara 5 tecken av SHA-1-hashen skickas till HIBP.
**Varför:** Etik och juridik, samt att en säkerhetsapp måste vara säker själv.
**Var:** `Checks/`, `Decorators/LoggingDecorator.cs`, README.

## D-008: Git-arbetsflöde och Definition of Done
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Branches `feat/<namn>-<funktion>`, aldrig direkt till `main`, alltid PR med granskning. Klart = bygger utan varningar, har tester, är granskat, resurser disposas.
**Varför:** Kodkvalitet, kunskapsdelning och spårbarhet för vem som gjort vad.
**Var:** Hela repot, `ci.yml`.

## D-009: Dokumentation i repot
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** `PROJECT_CONTEXT.md` och `DECISIONS.md` ligger i `docs/` i repot istället för i Word.
**Varför:** Versionshanteras, båda har alltid samma version, och de kan ges direkt till våra AI-assistenter.
**Var:** `docs/`.

## D-010: Fördjupning är steg 2
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Fördjupningar (live-resultat, BenchmarkDotNet, retry/caching-decorators, viktad poäng) görs först när alla fyra kontroller och Blazor-gränssnittet fungerar.
**Varför:** Säkra kraven först, sedan djup.
**Var:** –

## D-011: HTTPS-omdirigering ersätter lösenordskontrollen
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Kontrollen för läckta lösenord stryks och ersätts av en kontroll av om `http://` omdirigeras till `https://`. Nya minimikontroller: TLS-certifikat + E-postskydd. Ersätter delar av D-002 och D-007.
**Varför:** I ett verkligt scenario vill en användare inte skriva in sitt lösenord på en okänd webbsida, och en säkerhetsapp bör inte be om det. Lösenordskontrollen passade dessutom inte i kontraktet, eftersom alla andra kontroller tar en domän som indata. HTTPS-omdirigering är en vanlig miss hos småföretag och lätt att förklara på klarspråk. TLS och e-postskydd blir minimum eftersom de skyddar mot de dyraste riskerna: tappade kunder och falska fakturor.
**Var:** `Checks/HttpsRedirectCheck.cs` (Joakim).

## D-012: GitHub + driftsättning till Azure App Service
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Koden ligger på GitHub. GitHub Actions bygger och testar vid varje PR och driftsätter till Azure App Service vid merge till `main`.
**Varför:** GitHub syns för framtida arbetsgivare. En hel CI/CD-kedja visar professionellt arbetssätt. Azure finns tillgängligt via högskolekontona.
**Var:** `.github/workflows/`, Azure App Service.

## D-013: Blazor Web App utan separat API
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Webbgränssnittet byggs som en Blazor Web App (interaktivitet på serversidan) som refererar `SecurityCheck.Core` direkt. Inget separat Web API.
**Varför:** Ingen annan klient än vår egen webbsida skulle anropa ett API. Separationen mellan logik och UI finns redan genom klassbiblioteket. Ett API hade gett extra arbete (endpoints, serialisering, CORS, två driftsättningar) utan att tillföra något till kursens fokus. Blazor låter oss skriva även gränssnittet i C# och har inbyggt stöd för live-uppdateringar. Ett API kan läggas till senare utan att ändra `Core`.
**Var:** `SecurityCheck.Web`.

## D-014: Säkerhetsskydd för vår egen app
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Appen ska ha domänvalidering med SSRF-skydd, rate limiting, HTTPS/HSTS, inga detaljerade fel i produktion, hemligheter i GitHub Secrets, säkerhetsheaders, begränsade utgående förfrågningar samt Dependabot. CodeQL väljs bort i första hand.
**Varför:** Eftersom appen gör förfrågningar mot adresser som användaren anger kan den missbrukas, t.ex. för att nå interna resurser (SSRF, OWASP Top 10) eller för att belasta andras sidor. En säkerhetsapp måste själv vara säker. SSRF-skydd och rate limiting påverkar kärnans design och byggs därför från start.
**Var:** Domänvalidering (placering ej beslutad), kod som startar skanning, `Program.cs`, App Service, `.github/`.

## D-015: Domänvalidering i en separat DomainValidator som anropas av scannern
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** En egen klass, `DomainValidator`, kontrollerar att domänen är säker (SSRF-skydd) en gång innan några kontroller körs. `SecurityScanner` anropar den först; nekas domänen körs inga kontroller och användaren får ett tydligt meddelande. Valideringen byggs med TDD.
**Varför:** En ogiltig domän är ogiltig för alla kontroller, så den ska bara kontrolleras en gång. Alternativen valdes bort: validering i varje kontroll ger duplicerad kod och risk att den glöms i nya kontroller; en `ValidationDecorator` validerar samma domän flera gånger, och decorators passar bättre för beteende som gäller varje enskild körning (som timeout). En separat klass ger scannern ett tydligt ansvar och är lätt att testa isolerat.
**Var:** `Validation/DomainValidator.cs`, `Scanning/SecurityScanner.cs`.

## D-016: Produktnamnet Webbesiktningen
**Datum:** 2026-10-13 · **Beslutat av:** Erik, Joakim
**Vad:** Appen heter Webbesiktningen i gränssnitt och README. Kod, namnrymder och projekt behåller engelska namn (`SecurityCheck.*`).
**Varför:** Metaforen med bilbesiktning förklarar appen direkt för målgruppen: en regelbunden kontroll som ger anmärkningar och åtgärder.
**Var:** Gränssnittet, `README.md`.

## D-017: CI på Linux med varningar som fel, Dependabot och skyddad main
**Datum:** 2026-10-13 · **Beslutat av:** Joakim, granskat av Erik
**Vad:** GitHub Actions kör restore, build (`Release`, `-warnaserror`) och test som separata steg vid varje PR och push till `main`, på `ubuntu-latest` med minsta möjliga behörighet (`contents: read`). Dependabot kontrollerar NuGet-paket och GitHub Actions varje vecka; minor- och patch-uppdateringar grupperas i en PR. `main` är skyddad: PR med ett godkännande och grön `build-and-test` krävs, och ingen kan gå förbi reglerna.
**Varför:** Gör Definition of Done (D-008) automatisk istället för beroende av att någon minns att kontrollera. Linux är snabbast och skiljer på stora och små bokstäver i sökvägar, så sådana fel upptäcks tidigt. Separata steg visar direkt vad som gick fel. Gratis eftersom repot är publikt. Branch protection gör regeln "aldrig direkt till `main`" teknisk istället för en överenskommelse.
**Var:** `.github/workflows/ci.yml`, `.github/dependabot.yml`, repoinställningar på GitHub.

## D-018: Fabriksmetoder i CheckResult
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** `CheckResult` har en privat konstruktor och skapas bara via `CheckResult.Completed(checkName, findings)` och `CheckResult.Failed(checkName, reason)`. Egenskaperna har bara `get` (inte `init`). Ersätter den positionella recorden i D-005; övriga delar av D-005 gäller.
**Varför:** `Status` och `FailureReason` måste stämma överens. Med en vanlig konstruktor går det att skapa motsägelsefulla resultat, t.ex. `Completed` med en felorsak eller `Failed` utan orsak. Fabriksmetoderna gör sådana resultat omöjliga att skapa, och `get` utan `init` hindrar att skyddet kringgås med `with`. Bortvalt: positionell record (för lätt att skapa ogiltiga resultat) och att härleda `Status` från `FailureReason` (mindre tydligt för läsaren). `Finding` behåller positionell form eftersom den inte har några ogiltiga kombinationer.
**Var:** `Contracts/CheckResult.cs`.

## D-019: Testkonventioner och jämförelse fält för fält
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** Testnamn följer `Metod_Scenario_FörväntatResultat` i PascalCase. Varje test följer Arrange–Act–Assert och testar ett scenario. Samma test med olika indata skrivs som `[Theory]` med `[InlineData]`. Testmappar speglar mapparna i `src`. `CheckResult` jämförs fält för fält, inte som helhet.
**Varför:** Enhetliga tester är lättare att läsa och granska, och ett tydligt namn visar vad som gick fel direkt i CI-loggen. Records jämför `IReadOnlyList<Finding>` som referens, så två resultat med samma fynd i olika listor blir inte lika med `Assert.Equal`. Listor i sig går att jämföra med `Assert.Equal`, eftersom xUnit jämför samlingar element för element.
**Var:** `tests/SecurityCheck.Tests/`.

## D-020: FailureReason.InvalidInput behålls som extra skydd
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** `InvalidInput` finns kvar i `FailureReason` trots att `DomainValidator` validerar domänen innan kontrollerna körs (D-015).
**Varför:** En kontroll kan anropas utan att gå via scannern, t.ex. i tester eller om ny kod läggs till senare. Då ska den ändå kunna svara att indata var ogiltig istället för att krascha. Bortvalt: att ta bort värdet, vilket hade gjort kontraktet mindre men förutsatt att valideringen alltid körs först.
**Var:** `Contracts/FailureReason.cs`, `Checks/`.

## D-021: DomainValidator returnerar ett eget resultat med meddelande
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** `DomainValidator` returnerar ett eget resultat som anger om domänen godkändes och, om inte, ett meddelande som kan visas för användaren. Kompletterar D-014 och D-015.
**Varför:** En nekad domän är ett förväntat utfall, inte ett fel, så undantag passar inte. En `bool` säger inte varför domänen nekades, och användaren behöver ett begripligt meddelande på klarspråk.
**Var:** `Validation/` (Erik).

## D-022: Kommentarer i koden får vara på svenska
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** Namn på klasser, metoder, variabler och namnrymder skrivs på engelska. Kommentarer, inklusive `///`-dokumentation, får vara på svenska. Ändrar regeln i `PROJECT_CONTEXT.md` avsnitt 12.
**Varför:** _(fyll i)_
**Var:** Hela koden, `PROJECT_CONTEXT.md` avsnitt 12.

## D-023: Arbetsfördelning efter kontraktet
**Datum:** 2026-10-14 · **Beslutat av:** Erik, Joakim
**Vad:** Kontraktet i `Contracts/` görs tillsammans, utom `ISecurityCheck` som Erik skriver i början av sin inspelning av Strategy-mönstret och lägger i en egen liten PR. Därefter arbetar var och en på egen hand enligt fördelningen i `PROJECT_CONTEXT.md` avsnitt 2 fram till Blazor-gränssnittet. Ingen ändrar i den andras filer utan att ägaren granskar.
**Varför:** Varje person ska spela in sitt eget mönster, och interfacet är grunden i Strategy-mönstret. En liten PR med bara interfacet gör att Joakim snabbt kan börja på sina decorators, som bygger på det. Tydligt ägarskap minskar merge-konflikter och visar vem som gjort vad.
**Var:** `PROJECT_CONTEXT.md` avsnitt 2, `Contracts/ISecurityCheck.cs`.

---

## Öppna frågor

- **Fast veckomöte:** ska vi ha en fast avstämning i veckan utöver den löpande kontakten?
- **Var kopplas decorators ihop med kontrollerna?** Förslag: i `Program.cs` i Cli och Web, så att `SecurityScanner` inte behöver känna till decorators.
- **Vad returnerar `SecurityScanner` till gränssnittet?** En lista med `CheckResult` eller en egen typ (t.ex. `ScanReport`) som också kan bära valideringsmeddelandet från `DomainValidator` (D-021)? Behöver bestämmas innan Blazor-vyn.
