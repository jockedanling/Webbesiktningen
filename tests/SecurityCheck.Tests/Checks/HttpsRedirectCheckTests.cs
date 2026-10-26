using NSubstitute;
using SecurityCheck.Core.Checks;
using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Tests.Checks;

public class HttpsRedirectCheckTests
{
    private const string Domain = "example.com";

    private readonly IRedirectFetcher _fetcher = Substitute.For<IRedirectFetcher>();

    [Fact]
    public async Task RunAsync_RedirectsToHttps_ReportsOnlyInfo()
    {
        // Arrange
        GivenResponse(301, "https://example.com/");
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.NotEmpty(result.Findings);
        Assert.All(result.Findings, f => Assert.Equal(Severity.Info, f.Severity));
    }

    [Theory]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task RunAsync_OtherRedirectCodesToHttps_ReportOnlyInfo(int statusCode)
    {
        // Arrange
        GivenResponse(statusCode, "https://example.com/");
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.NotEmpty(result.Findings);
        Assert.All(result.Findings, f => Assert.Equal(Severity.Info, f.Severity));
    }

    [Fact]
    public async Task RunAsync_RedirectsToHttp_ReportsMediumFinding()
    {
        // Arrange
        GivenResponse(301, "http://www.example.com/");
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Severity == Severity.Medium);
    }

    [Fact]
    public async Task RunAsync_RedirectsToRelativeAddress_ReportsMediumFinding()
    {
        // Arrange
        _fetcher.FetchAsync(Domain, Arg.Any<CancellationToken>())
            .Returns(new RedirectResponse(301, new Uri("/start", UriKind.Relative)));
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Severity == Severity.Medium);
    }

    [Fact]
    public async Task RunAsync_NoRedirect_ReportsHighFinding()
    {
        // Arrange
        GivenResponse(200, location: null);
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.Contains(result.Findings, f => f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_RedirectWithoutLocation_ReportsHighFinding()
    {
        // Arrange
        GivenResponse(301, location: null);
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Contains(result.Findings, f => f.Severity == Severity.High);
    }

    [Fact]
    public async Task RunAsync_ConnectionFails_ReturnsFailedUnreachable()
    {
        // Arrange
        _fetcher.FetchAsync(Domain, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RedirectResponse>(new HttpRequestException("Ingen anslutning")));
        var check = new HttpsRedirectCheck(_fetcher);
        // Act
        var result = await check.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Unreachable, result.FailureReason);
        Assert.Equal(check.Name, result.CheckName);
    }

    private void GivenResponse(int statusCode, string? location) =>
        _fetcher.FetchAsync(Domain, Arg.Any<CancellationToken>())
            .Returns(new RedirectResponse(statusCode, location is null ? null : new Uri(location)));
}
