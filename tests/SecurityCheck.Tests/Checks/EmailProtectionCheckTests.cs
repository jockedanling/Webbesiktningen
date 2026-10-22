using NSubstitute;
using SecurityCheck.Core.Checks;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Tests.Checks;

public class EmailProtectionCheckTests
{
    private const string Domain = "example.com";
    private const string DmarcName = "_dmarc.example.com";
    private const string GoodSpf = "v=spf1 include:_spf.example.net -all";
    private const string GoodDmarc = "v=DMARC1; p=reject";

    private readonly ITxtRecordResolver _resolver = Substitute.For<ITxtRecordResolver>();

    [Fact]
    public async Task RunAsync_DmarcMissing_ReportsHighFinding()
    {
        // Arrange
        GivenTxtRecords(Domain, GoodSpf);
        GivenTxtRecords(DmarcName);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.Contains(result.Findings, f => f.Title == "DMARC saknas" && f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_SpfAndDmarcGood_ReportsOnlyInfo()
    {
        // Arrange
        GivenTxtRecords(Domain, GoodSpf);
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.NotEmpty(result.Findings);
        Assert.All(result.Findings, f => Assert.Equal(Severity.Info, f.Severity));
    }

    [Theory]
    [InlineData("v=DMARC1; p=none", Severity.Medium)]
    [InlineData("v=DMARC1; p=quarantine", Severity.Info)]
    [InlineData("v=DMARC1; p=reject", Severity.Info)]
    [InlineData("v=DMARC1; p=reject; sp=none", Severity.Info)]
    public async Task RunAsync_DmarcPolicy_ReportsExpectedSeverity(string dmarcRecord, Severity expected)
    {
        // Arrange
        GivenTxtRecords(Domain, GoodSpf);
        GivenTxtRecords(DmarcName, dmarcRecord);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Title.StartsWith("DMARC") && f.Severity == expected);
    }

    [Fact]
    public async Task RunAsync_SpfMissing_ReportsHighFinding()
    {
        // Arrange
        GivenTxtRecords(Domain);
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Title == "SPF saknas" && f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_SpfAllowsEveryone_ReportsHighFinding()
    {
        // Arrange
        GivenTxtRecords(Domain, "v=spf1 +all");
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Title == "SPF tillåter alla avsändare" && f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_MultipleSpfRecords_ReportsHighFinding()
    {
        // Arrange
        GivenTxtRecords(Domain, GoodSpf, "v=spf1 include:_spf.other.net -all");
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Title == "Flera SPF-poster" && f.Severity == Severity.High);
    }
    [Fact]
    public async Task RunAsync_OtherTxtRecords_AreIgnored()
    {
        // Arrange
        GivenTxtRecords(Domain, "google-site-verification=abc123", GoodSpf, "MS=ms12345678");
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.All(result.Findings, f => Assert.Equal(Severity.Info, f.Severity));
    }

    [Fact]
    public async Task RunAsync_SpfInUpperCase_IsRecognized()
    {
        // Arrange
        GivenTxtRecords(Domain, "V=SPF1 -ALL");
        GivenTxtRecords(DmarcName, GoodDmarc);
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.DoesNotContain(result.Findings, f => f.Title == "SPF saknas");
    }

    [Fact]
    public async Task RunAsync_DnsFails_ReturnsFailedUnreachable()
    {
        // Arrange
        _resolver.GetTxtRecordsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<string>>(new IOException("DNS-servern svarade inte")));
        var check = new EmailProtectionCheck(_resolver);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Unreachable, result.FailureReason);
        Assert.Equal(check.Name, result.CheckName);
    }

    private void GivenTxtRecords(string name, params string[] records) =>
        _resolver.GetTxtRecordsAsync(name, Arg.Any<CancellationToken>()).Returns(records);
}