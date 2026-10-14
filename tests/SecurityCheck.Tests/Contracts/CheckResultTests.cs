using SecurityCheck.Core.Contracts;

namespace SecurityCheck.Tests.Contracts;

public class CheckResultTests
{
    [Fact]
    public void Completed_SetsStatusCompletedAndNoFailureReason()
    {
        // Arrange
        var findings = new List<Finding>();
        // Act
        var result = CheckResult.Completed("TLS", findings);
        // Assert
        Assert.Equal(CheckStatus.Completed, result.Status);
        Assert.Null(result.FailureReason);
    }
    [Fact]
    public void Completed_KeepsGivenFindings()
    {
        // Arrange
        var finding = new Finding("DMARC saknades", Severity.High, "Förklaring", "Åtgärd");
        var findings = new List<Finding> { finding };
        // Act
        var result = CheckResult.Completed("Email", findings);
        // Assert
        Assert.Equal(findings, result.Findings);
    }
    [Theory]
    [InlineData(FailureReason.Timeout)]
    [InlineData(FailureReason.InvalidInput)]
    [InlineData(FailureReason.Unreachable)]
    [InlineData(FailureReason.Unexpected)]
    public void Failed_SetsStatusFailedAndGivenReason(FailureReason reason)
    {
        // Act
        var result = CheckResult.Failed("TLS", reason);
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Equal(reason, result.FailureReason);

    }
    [Fact]
    public void Failed_HasNoFindings()
    {
        // Arrange
        //var findings = new List<Finding>();
        // Act
        var result = CheckResult.Failed("TLS", FailureReason.Timeout);
        // Assert
        Assert.Empty(result.Findings);

    }
}
