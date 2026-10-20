using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Decorators;

namespace SecurityCheck.Tests.Decorators;

public class ErrorHandlingDecoratorTests
{
    private const string CheckName = "Fejkkontroll";
    private const string Domain = "Example.com";

    private readonly ISecurityCheck _inner = Substitute.For<ISecurityCheck>();
    private readonly FakeLogger<ErrorHandlingDecorator> _logger = new();

    public ErrorHandlingDecoratorTests()
    {
        _inner.Name.Returns(CheckName);
    }


    // Ett test för misslyckad Task blir Failed(Unexpected).
    [Fact]
    public async Task RunAsync_InnerReturnsFaultedTask_ReturnsFailedUnexpected()
    {
        // Arrange
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
        .Returns(Task.FromException<CheckResult>(new InvalidOperationException("Bugg")));
        var decorator = new ErrorHandlingDecorator(_inner, _logger);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Unexpected, result.FailureReason);
        Assert.Equal(CheckName, result.CheckName);
    }
    // Undantaget loggas
    [Fact]
    public async Task RunAsync_InnerThrows_LogsErrorWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Bugg");
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
        .Returns(Task.FromException<CheckResult>(exception));
        var decorator = new ErrorHandlingDecorator(_inner, _logger);
        // Act
        await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Same(exception, record.Exception); // Kontrollerar att det är exakt samma undantagsobjekt som loggas
        Assert.Equal(Domain, record.GetStructuredStateValue("Domain"));
    }
    // Anroparen avbryter => undantaget kastas vidare
    [Fact]
    public async Task RunAsync_CallerCancels_ThrowsAndLogsNothing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _inner.RunAsync(Domain, cts.Token)
        .Returns(Task.FromCanceled<CheckResult>(cts.Token));
        var decorator = new ErrorHandlingDecorator(_inner, _logger);
        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decorator.RunAsync(Domain, cts.Token));
        Assert.Empty(_logger.Collector.GetSnapshot()
    );
    }
    [Fact]
    public async Task RunAsync_InnerCancelsWithoutCallerCancel_ReturnsFailedUnexpected()
    {
        // Arrange
_inner.RunAsync(Domain, Arg.Any<CancellationToken>())
.Returns(Task.FromException<CheckResult>(new TaskCanceledException("HttpClient-timeout")));
var decorator = new ErrorHandlingDecorator(_inner, _logger);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);

        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Unexpected, result.FailureReason);
    }

}