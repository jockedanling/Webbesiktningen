using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Decorators;

namespace SecurityCheck.Tests.Decorators;

public class LoggingDecoratorTests
{
    private const string CheckName = "Fejkkontroll";
    private const string Domain = "example.com";

    private readonly ISecurityCheck _inner = Substitute.For<ISecurityCheck>();
    private readonly FakeLogger<LoggingDecorator> _logger = new();

    public LoggingDecoratorTests()
    {
        _inner.Name.Returns(CheckName);
    }

    [Fact]
    public void Name_ReturnsInnerName()
    {
        // Arrange
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        var name = decorator.Name;
        // Assert
        Assert.Equal(CheckName, name);
    }

    [Fact]
    public async Task RunAsync_InnerCompletes_ReturnsSameResult()
    {
        // Arrange
        var expected = CheckResult.Completed(CheckName, []);
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>()).Returns(expected);
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RunAsync_InnerFails_ReturnsSameResult()
    {
        // Arrange
        var expected = CheckResult.Failed(CheckName, FailureReason.Timeout);
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>()).Returns(expected);
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RunAsync_ForwardsDomainAndToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _inner.RunAsync(Domain, cts.Token).Returns(CheckResult.Completed(CheckName, []));
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        await decorator.RunAsync(Domain, cts.Token);
        // Assert
        await _inner.Received(1).RunAsync(Domain, cts.Token);
    }

    [Fact]
    public async Task RunAsync_InnerCompletes_LogsInformationWithFindingCount()
    {
        // Arrange
        var finding = new Finding("DMARC saknas", Severity.High, "Förklaring", "Åtgärd");
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
            .Returns(CheckResult.Completed(CheckName, [finding]));
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        var records = _logger.Collector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Equal(LogLevel.Debug, records[0].Level);

        var completed = records[1];
        Assert.Equal(LogLevel.Information, completed.Level);
        Assert.Equal(CheckName, completed.GetStructuredStateValue("CheckName"));
        Assert.Equal("1", completed.GetStructuredStateValue("FindingCount"));
    }

    [Fact]
    public async Task RunAsync_InnerFails_LogsWarningWithFailureReason()
    {
        // Arrange
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
            .Returns(CheckResult.Failed(CheckName, FailureReason.Unreachable));
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        var records = _logger.Collector.GetSnapshot();
        Assert.Equal(2, records.Count);
        Assert.Equal(LogLevel.Debug, records[0].Level);

        var failed = records[1];
        Assert.Equal(LogLevel.Warning, failed.Level);
        Assert.Equal(nameof(FailureReason.Unreachable), failed.GetStructuredStateValue("FailureReason"));
    }

    [Fact]
    public async Task RunAsync_CallerCancels_ThrowsAndLogsOnlyStart()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _inner.RunAsync(Domain, cts.Token)
            .Returns(Task.FromCanceled<CheckResult>(cts.Token));
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decorator.RunAsync(Domain, cts.Token));

        var start = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Debug, start.Level);
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNull()
    {
        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new LoggingDecorator(null!, _logger));
    }

    [Fact]
    public async Task RunAsync_InnerCompletes_DoesNotLogFindingContent()
    {
        // Arrange
        var finding = new Finding("DMARC saknas", Severity.High, "Förklaring", "Åtgärd");
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
        .Returns(CheckResult.Completed(CheckName, [finding]));
        var decorator = new LoggingDecorator(_inner, _logger);
        // Act
        await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        foreach(var record in _logger.Collector.GetSnapshot())
        {
            Assert.DoesNotContain(finding.Title, record.Message);
            Assert.DoesNotContain(finding.Explanation, record.Message);
            Assert.DoesNotContain(finding.Recommendation, record.Message);
        }
    }
}