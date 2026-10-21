using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SecurityCheck.Core.Checks;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Tests.Checks;

public class TlsCertificateCheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private readonly ICertificateFetcher _fetcher;
    private readonly TlsCertificateCheck _check;

    public TlsCertificateCheckTests()
    {
        _fetcher = Substitute.For<ICertificateFetcher>();

        // Klockan står still i testerna, så att antal dagar kvar blir förutsägbart
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);

        _check = new TlsCertificateCheck(_fetcher, time);
    }

    [Fact]
    public async Task RunAsync_ValidCertificate_ReturnsCompletedWithSingleInfoFinding()
    {
        // Arrange
        ReturnCertificate(daysLeft: 60);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.Equal(_check.Name, result.CheckName);
        var finding = Assert.Single(result.Findings);
        Assert.Equal(Severity.Info, finding.Severity);
    }

    // Let's Encrypt förnyar vid 30 dagar kvar, så bara de sista två veckorna ger en varning
    [Theory]
    [InlineData(1, Severity.High)]
    [InlineData(7, Severity.High)]
    [InlineData(8, Severity.Medium)]
    [InlineData(14, Severity.Medium)]
    [InlineData(15, Severity.Info)]
    public async Task RunAsync_DaysLeft_ReturnsFindingWithExpectedSeverity(int daysLeft, Severity expected)
    {
        // Arrange
        ReturnCertificate(daysLeft);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        var finding = Assert.Single(result.Findings);
        Assert.Equal(expected, finding.Severity);
    }

    [Fact]
    public async Task RunAsync_ExpiredCertificate_ReturnsSingleHighFinding()
    {
        // Arrange
        ReturnCertificate(daysLeft: -3);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        var finding = Assert.Single(result.Findings);
        Assert.Equal(Severity.High, finding.Severity);
    }

    [Fact]
    public async Task RunAsync_CertificateNotYetValid_ReturnsHighFinding()
    {
        // Arrange
        _fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CertificateInfo(Now.AddDays(2), Now.AddDays(90), SslPolicyErrors.None));

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Contains(result.Findings, f => f.Severity == Severity.High);
    }

    [Theory]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch)] // certifikatet gäller ett annat namn
    [InlineData(SslPolicyErrors.RemoteCertificateChainErrors)]  // inte betrott, t.ex. självsignerat
    public async Task RunAsync_CertificateError_ReturnsHighFinding(SslPolicyErrors errors)
    {
        // Arrange
        ReturnCertificate(daysLeft: 60, errors);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.Contains(result.Findings, f => f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_SeveralProblems_ReturnsOneHighFindingPerProblem()
    {
        // Arrange
        ReturnCertificate(daysLeft: -3,
            SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Findings.Count);
        Assert.All(result.Findings, f => Assert.Equal(Severity.High, f.Severity));
    }

    // Sajten svarar men kan inte erbjuda en säker anslutning: ett fynd, inte ett misslyckande (D-024)
    [Fact]
    public async Task RunAsync_HandshakeFails_ReturnsCompletedWithHighFinding()
    {
        // Arrange
        _fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException());

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        var finding = Assert.Single(result.Findings);
        Assert.Equal(Severity.High, finding.Severity);
    }

    // Sajten svarar inte: säger inget om certifikatet, därför Failed (D-024)
    [Theory]
    [InlineData(typeof(SocketException))]
    [InlineData(typeof(IOException))]
    public async Task RunAsync_ConnectionFails_ReturnsFailedUnreachable(Type exceptionType)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        _fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _check.RunAsync("example.com", CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Unreachable, result.FailureReason);
    }

    [Fact]
    public async Task RunAsync_CallerCancels_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _check.RunAsync("example.com", cts.Token));
    }

    [Fact]
    public async Task RunAsync_AnyDomain_PassesDomainAndTokenToFetcher()
    {
        // Arrange
        ReturnCertificate(daysLeft: 60);
        using var cts = new CancellationTokenSource();

        // Act
        await _check.RunAsync("example.com", cts.Token);

        // Assert
        await _fetcher.Received(1).FetchAsync("example.com", cts.Token);
    }

    private void ReturnCertificate(int daysLeft, SslPolicyErrors errors = SslPolicyErrors.None)
    {
        _fetcher.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CertificateInfo(Now.AddDays(-30), Now.AddDays(daysLeft), errors));
    }
}
