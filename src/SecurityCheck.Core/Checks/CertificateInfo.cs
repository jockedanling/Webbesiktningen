using System.Net.Security;

namespace SecurityCheck.Core.Checks;

/// <summary>
/// Det TlsCertificateCheck behöver veta om ett certifikat. En enkel record istället för X509Certificate2,
/// som måste disposas och är svår att skapa i tester.
/// </summary>
public sealed record CertificateInfo(
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    SslPolicyErrors Errors); // None om certifikatet är betrott och gäller rätt namn
