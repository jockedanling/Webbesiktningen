using System.Net;
using NSubstitute;
using SecurityCheck.Core.Validation;

namespace SecurityCheck.Tests.Validation;

public class DomainValidatorTests
{
    private readonly IDnsResolver _dns;
    private readonly DomainValidator _validator;

    public DomainValidatorTests()
    {
        // Som standard pekar alla domäner på en publik adress, så att bara indata avgör utfallet
        _dns = Substitute.For<IDnsResolver>();
        _dns.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("93.184.215.14")]);
        _validator = new DomainValidator(_dns);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("www.example.se")]
    [InlineData("Example.COM")]
    [InlineData("åkeri.se")]
    public async Task ValidateAsync_ValidDomain_ReturnsValid(string domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task ValidateAsync_EmptyInput_ReturnsInvalid(string? domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain!, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LocalHost")]
    [InlineData("app.localhost")]
    public async Task ValidateAsync_Localhost_ReturnsInvalid(string domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    // Alla IP-adresser nekas, även publika, eftersom appen kontrollerar domäner
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("8.8.8.8")]
    [InlineData("::1")]
    [InlineData("[::1]")]
    [InlineData("2001:4860:4860::8888")]
    public async Task ValidateAsync_IpAddress_ReturnsInvalid(string domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    // Hela adresser nekas tills vidare; att plocka ut domänen är en möjlig förbättring senare
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/")]
    [InlineData("example.com/kontakt")]
    [InlineData("example.com:8080")]
    public async Task ValidateAsync_FullUrl_ReturnsInvalid(string domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    // Namn utan punkt, t.ex. "intranet", kan slås upp i ett internt nätverk och nekas därför
    [Theory]
    [InlineData("intranet")]
    [InlineData("exa mple.com")]
    [InlineData("example..com")]
    [InlineData(".example.com")]
    public async Task ValidateAsync_InvalidFormat_ReturnsInvalid(string domain)
    {
        // Act
        var result = await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    // Ogiltig indata ska stoppas innan någon förfrågan lämnar appen
    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("https://example.com")]
    public async Task ValidateAsync_RejectedInput_DoesNotLookUpDns(string domain)
    {
        // Act
        await _validator.ValidateAsync(domain, CancellationToken.None);

        // Assert
        await _dns.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Steg 2: DNS. En giltig domän kan peka på en intern adress, och då ska den nekas (SSRF-skydd)
    [Theory]
    [InlineData("0.0.0.0")]               // "den här datorn"
    [InlineData("10.0.0.1")]              // privat nätverk
    [InlineData("100.64.0.1")]            // operatörens delade nät (CGNAT)
    [InlineData("127.0.0.1")]             // loopback
    [InlineData("169.254.169.254")]       // link-local, molnets metadatatjänst
    [InlineData("172.16.0.1")]            // privat nätverk
    [InlineData("192.168.1.1")]           // privat nätverk
    [InlineData("224.0.0.1")]             // multicast
    [InlineData("255.255.255.255")]       // broadcast
    [InlineData("::")]                    // ospecificerad
    [InlineData("::1")]                   // loopback
    [InlineData("fc00::1")]               // privat nätverk (unique local)
    [InlineData("fe80::1")]               // link-local
    [InlineData("ff02::1")]               // multicast
    [InlineData("::ffff:127.0.0.1")]      // IPv4-adress förklädd som IPv6
    [InlineData("::ffff:10.0.0.1")]       // IPv4-adress förklädd som IPv6
    public async Task ValidateAsync_ResolvesToNonPublicAddress_ReturnsInvalid(string address)
    {
        // Arrange
        ResolveExampleComTo(address);

        // Act
        var result = await _validator.ValidateAsync("example.com", CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_OneOfSeveralAddressesIsNonPublic_ReturnsInvalid()
    {
        // Arrange
        // Annars kan en angripare blanda in en intern adress bland publika
        ResolveExampleComTo("93.184.215.14", "10.0.0.1");

        // Act
        var result = await _validator.ValidateAsync("example.com", CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ResolvesToPublicAddresses_ReturnsValid()
    {
        // Arrange
        ResolveExampleComTo("93.184.215.14", "2606:2800:21f:cb07:6820:80da:af6b:8b2c");

        // Act
        var result = await _validator.ValidateAsync("example.com", CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_DomainNotFound_ReturnsInvalid()
    {
        // Arrange
        // IDnsResolver returnerar en tom lista när domänen inte finns
        ResolveExampleComTo();

        // Act
        var result = await _validator.ValidateAsync("example.com", CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ValidDomain_PassesTrimmedDomainAndTokenToResolver()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _validator.ValidateAsync("  example.com  ", cts.Token);

        // Assert
        await _dns.Received(1).ResolveAsync("example.com", cts.Token);
    }

    // Ersätter standardsvaret från konstruktorn för just example.com
    private void ResolveExampleComTo(params string[] addresses)
    {
        _dns.ResolveAsync("example.com", Arg.Any<CancellationToken>())
            .Returns([.. addresses.Select(IPAddress.Parse)]);
    }
}
