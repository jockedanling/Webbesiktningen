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
}
