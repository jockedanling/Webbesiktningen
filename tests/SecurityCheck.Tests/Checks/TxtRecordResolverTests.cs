using SecurityCheck.Core.Checks;

namespace SecurityCheck.Tests.Checks;

// Integrationstester mot riktig DNS, eftersom TxtRecordResolver bara är ett tunt lager runt DnsClient.
// google.com används eftersom domänen har både SPF och DMARC och knappast tar bort dem.
[Trait("Category", "Integration")]
public class TxtRecordResolverTests
{
    private readonly TxtRecordResolver _resolver = new();

    [Fact]
    public async Task GetTxtRecordsAsync_DomainWithSpf_ReturnsSpfRecord()
    {
        // Act
        var records = await _resolver.GetTxtRecordsAsync("google.com", CancellationToken.None);

        // Assert
        Assert.Contains(records, r => r.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTxtRecordsAsync_DmarcName_ReturnsDmarcRecord()
    {
        // Act
        var records = await _resolver.GetTxtRecordsAsync("_dmarc.google.com", CancellationToken.None);

        // Assert
        Assert.Contains(records, r => r.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTxtRecordsAsync_NonExistentName_ReturnsEmpty()
    {
        // Act
        var records = await _resolver.GetTxtRecordsAsync("finns-inte-webbesiktningen.example.com", CancellationToken.None);

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public async Task GetTxtRecordsAsync_CallerCancels_Throws()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _resolver.GetTxtRecordsAsync("google.com", cts.Token));
    }
}
