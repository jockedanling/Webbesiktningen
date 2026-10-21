using System.Net;
using SecurityCheck.Core.Validation;

namespace SecurityCheck.Tests.Validation;

// Testar mot systemets riktiga DNS, eftersom DnsResolver bara är ett tunt lager runt Dns-klassen
public class DnsResolverTests
{
    private readonly DnsResolver _resolver = new();

    [Fact]
    public async Task ResolveAsync_Localhost_ReturnsLoopbackAddress()
    {
        // Act
        var addresses = await _resolver.ResolveAsync("localhost", CancellationToken.None);

        // Assert
        Assert.Contains(addresses, IPAddress.IsLoopback);
    }

    [Fact]
    public async Task ResolveAsync_DomainDoesNotExist_ReturnsEmpty()
    {
        // Act
        // .invalid är reserverat och finns garanterat inte (RFC 6761)
        var addresses = await _resolver.ResolveAsync("does-not-exist.invalid", CancellationToken.None);

        // Assert
        Assert.Empty(addresses);
    }

    [Fact]
    public async Task ResolveAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _resolver.ResolveAsync("example.com", cts.Token));
    }
}
