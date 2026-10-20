using SecurityCheck.Core.Validation;

namespace SecurityCheck.Tests.Validation;

public class DomainValidationResultTests
{
    [Fact]
    public void Valid_NoArguments_SetsIsValidTrueAndNoMessage()
    {
        // Act
        var result = DomainValidationResult.Valid();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Invalid_WithMessage_SetsIsValidFalseAndKeepsMessage()
    {
        // Arrange
        var message = "Domänen pekar på en intern adress och kan inte kontrolleras.";

        // Act
        var result = DomainValidationResult.Invalid(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(message, result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Invalid_EmptyOrWhiteSpaceMessage_ThrowsArgumentException(string message)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DomainValidationResult.Invalid(message));
    }

    [Fact]
    public void Invalid_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DomainValidationResult.Invalid(null!));
    }
}
