using NSubstitute;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Scanning;
using SecurityCheck.Core.Validation;

namespace SecurityCheck.Tests.Scanning;

public class SecurityScannerTests
{
    [Fact]
    public async Task ScanAsync_DomainRejected_RunsNoChecks()
    {
        // Arrange
        var validator = Substitute.For<IDomainValidator>();
        validator.ValidateAsync("localhost", Arg.Any<CancellationToken>())
            .Returns(DomainValidationResult.Invalid("Domänen är inte tillåten."));
        var check = Substitute.For<ISecurityCheck>();
        var scanner = new SecurityScanner([check], validator);

        // Act
        var report = await scanner.ScanAsync("localhost");

        // Assert
        Assert.False(report.IsDomainAccepted);
        Assert.Equal("Domänen är inte tillåten.", report.RejectionMessage);
        Assert.Empty(report.Results);
        await check.DidNotReceive().RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanAsync_DomainAccepted_ReturnsResultsInCheckOrder()
    {
        // Arrange
        var tls = CreateCheck("TLS");
        var headers = CreateCheck("Headers");
        var scanner = new SecurityScanner([tls, headers], CreateAcceptingValidator());

        // Act
        var report = await scanner.ScanAsync("example.com");

        // Assert
        Assert.True(report.IsDomainAccepted);
        Assert.Null(report.RejectionMessage);
        Assert.Equal(["TLS", "Headers"], report.Results.Select(r => r.CheckName));
    }

    [Fact]
    public async Task ScanAsync_DomainAccepted_PassesDomainAndTokenToChecks()
    {
        // Arrange
        var check = CreateCheck("TLS");
        var scanner = new SecurityScanner([check], CreateAcceptingValidator());
        using var cts = new CancellationTokenSource();

        // Act
        await scanner.ScanAsync("example.com", cts.Token);

        // Assert
        await check.Received(1).RunAsync("example.com", cts.Token);
    }

    [Fact]
    public async Task ScanAsync_MultipleChecks_StartsSecondCheckBeforeFirstCompletes()
    {
        // Arrange
        // Den första kontrollen blir inte klar förrän testet släpper den
        var firstCanFinish = new TaskCompletionSource<CheckResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Substitute.For<ISecurityCheck>();
        first.RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(firstCanFinish.Task);

        // Den andra kontrollen signalerar när den startar
        var secondStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var second = Substitute.For<ISecurityCheck>();
        second.RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                secondStarted.SetResult();
                return Task.FromResult(CheckResult.Completed("Headers", []));
            });

        var scanner = new SecurityScanner([first, second], CreateAcceptingValidator());

        // Act
        var scanTask = scanner.ScanAsync("example.com");

        // Assert
        // Körs kontrollerna i tur och ordning startar den andra aldrig, och WaitAsync kastar TimeoutException
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(scanTask.IsCompleted);

        firstCanFinish.SetResult(CheckResult.Completed("TLS", []));
        var report = await scanTask;
        Assert.Equal(["TLS", "Headers"], report.Results.Select(r => r.CheckName));
    }

    private static IDomainValidator CreateAcceptingValidator()
    {
        var validator = Substitute.For<IDomainValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DomainValidationResult.Valid());
        return validator;
    }

    private static ISecurityCheck CreateCheck(string name)
    {
        var check = Substitute.For<ISecurityCheck>();
        check.Name.Returns(name);
        check.RunAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CheckResult.Completed(name, []));
        return check;
    }
}
