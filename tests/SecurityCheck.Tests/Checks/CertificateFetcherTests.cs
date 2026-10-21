using System.Net.Security;
using System.Net.Sockets;
using SecurityCheck.Core.Checks;

namespace SecurityCheck.Tests.Checks;

// Integrationstester mot riktiga sajter, eftersom CertificateFetcher bara är ett tunt lager runt TcpClient och SslStream.
// badssl.com har sajter med trasiga certifikat just för att testa sådant här.
[Trait("Category", "Integration")]
public class CertificateFetcherTests
{
    private readonly CertificateFetcher _fetcher = new();

    [Fact]
    public async Task FetchAsync_ValidCertificate_ReturnsNoErrorsAndFutureExpiry()
    {
        // Act
        var certificate = await _fetcher.FetchAsync("github.com", CancellationToken.None);

        // Assert
        Assert.Equal(SslPolicyErrors.None, certificate.Errors);
        Assert.True(certificate.NotAfter > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task FetchAsync_ExpiredCertificate_ReturnsPastExpiryWithoutChainErrors()
    {
        // Act
        var certificate = await _fetcher.FetchAsync("expired.badssl.com", CancellationToken.None);

        // Assert
        // Ett utgånget certifikat ska bara synas på datumet, inte också som "inte betrott"
        Assert.True(certificate.NotAfter < DateTimeOffset.UtcNow);
        Assert.False(certificate.Errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public async Task FetchAsync_SelfSignedCertificate_ReturnsChainErrors()
    {
        // Act
        var certificate = await _fetcher.FetchAsync("self-signed.badssl.com", CancellationToken.None);

        // Assert
        Assert.True(certificate.Errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public async Task FetchAsync_WrongHost_ReturnsNameMismatch()
    {
        // Act
        var certificate = await _fetcher.FetchAsync("wrong.host.badssl.com", CancellationToken.None);

        // Assert
        Assert.True(certificate.Errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    [Fact]
    public async Task FetchAsync_DomainDoesNotExist_ThrowsSocketException()
    {
        // Act & Assert
        // .invalid är reserverat och finns garanterat inte (RFC 6761)
        await Assert.ThrowsAnyAsync<SocketException>(
            () => _fetcher.FetchAsync("does-not-exist.invalid", CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _fetcher.FetchAsync("github.com", cts.Token));
    }
}
