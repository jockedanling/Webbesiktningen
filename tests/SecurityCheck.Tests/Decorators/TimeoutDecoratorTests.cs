using NSubstitute;
using SecurityCheck.Core.Contracts;
using SecurityCheck.Core.Decorators;

public class TimeoutDecoratorTests
{
    private const string CheckName = "Fejkkontroll";
    private const string Domain = "example.com";

    private static readonly TimeSpan LongTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    private readonly ISecurityCheck _inner = Substitute.For<ISecurityCheck>();

    public TimeoutDecoratorTests()
    {
        _inner.Name.Returns(CheckName);
    }

    // Den inre kontrollen väntar tills tokenen den får avbryts, som en långsam sajt
    private void InnerWaitsUntilCancelled()
    {
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
        .Returns(async callInfo =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>()); // Hämtar den token som decoratorn faktiskt skicka in.
            return CheckResult.Completed(CheckName, []);
        });
    }

    // Klar i tid -> samma resultat
    [Fact]
    public async Task RunAsync_InnerCompletesInTime_ReturnsSameResult()
    {
        // Arrange
        var expected = CheckResult.Completed(CheckName, []);
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>()).Returns(expected);
        var decorator = new TimeoutDecorator(_inner, LongTimeout);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Same(expected, result);
    }

    // För långsam -> Failed(Timeout)
    [Fact]
    public async Task RunAsync_InnerTooSlow_ReturnsFailedTimeout()
    {
        // Arrange
        InnerWaitsUntilCancelled();
        var decorator = new TimeoutDecorator(_inner, ShortTimeout);
        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(FailureReason.Timeout, result.FailureReason);
        Assert.Equal(CheckName, result.CheckName);
    }

    // Anroparen avbryter -> undantaget kastas vidare
    [Fact]
    public async Task RunAsync_CallerCancels_Throws()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        InnerWaitsUntilCancelled();
        var decorator = new TimeoutDecorator(_inner, LongTimeout);
        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decorator.RunAsync(Domain, cts.Token)
        );

    }

    // Den inre kontrollen avbryter själv -> kastas vidare, inte Timeout
    [Fact]
    public async Task RunAsync_InnerCancelsByItself_Throws()
    {
        // Arrange
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>())
        .Returns(Task.FromException<CheckResult>(new TaskCanceledException("HttpClient-timeout")));
        var decorator = new TimeoutDecorator(_inner, LongTimeout);
        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decorator.RunAsync(Domain, CancellationToken.None)
        );
    }

    [Fact(Timeout = 5000)] // Stoppar testet efter 5 sekunder och visar det som rött.
    public async Task RunAsync_InnerIgnoresToken_ReturnsFailedTimeout()
    {
        // Arrange
        var neverCompletes = new TaskCompletionSource<CheckResult>(); // TaskCompletionSource = En task som aldrig blir klar och inte bryr sig om någon token.
        _inner.RunAsync(Domain, Arg.Any<CancellationToken>()).Returns(neverCompletes.Task);
        var decorator = new TimeoutDecorator(_inner, ShortTimeout);

        // Act
        var result = await decorator.RunAsync(Domain, CancellationToken.None);
        // Assert
        Assert.Equal(FailureReason.Timeout, result.FailureReason);
    }
}