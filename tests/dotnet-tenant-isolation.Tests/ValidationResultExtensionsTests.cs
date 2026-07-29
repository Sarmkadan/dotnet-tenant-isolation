using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TenantIsolation.Configuration;

namespace TenantIsolation.Configuration;

public class ValidationResultExtensionsTests
{
    [Fact]
    public void Combine_MultipleResults_CombinesCorrectly()
    {
        // Arrange
        var result1 = new ValidationResult { IsValid = true };
        result1.Errors.Add("Error1");
        result1.IsValid = false;
        
        var result2 = new ValidationResult { IsValid = true };
        result2.Warnings.Add("Warning1");

        // Act
        var combined = new[] { result1, result2 }.Combine();

        // Assert
        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().ContainSingle().Which.Should().Be("Error1");
        combined.Warnings.Should().ContainSingle().Which.Should().Be("Warning1");
    }

    [Fact]
    public void AddError_ValidInput_AddsErrorAndSetsInvalid()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.AddError("Test error", "Property1");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("[Property1] Test error");
    }

    [Fact]
    public void AddWarning_ValidInput_AddsWarning()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.AddWarning("Test warning", "Property1");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle().Which.Should().Be("[Property1] Test warning");
    }

    [Fact]
    public void HasErrors_WithErrors_ReturnsTrue()
    {
        // Arrange
        var result = new ValidationResult();
        result.Errors.Add("Error");

        // Act & Assert
        result.HasErrors().Should().BeTrue();
    }

    [Fact]
    public void GetFirstError_WithErrors_ReturnsFirstError()
    {
        // Arrange
        var result = new ValidationResult();
        result.Errors.Add("Error1");
        result.Errors.Add("Error2");

        // Act & Assert
        result.GetFirstError().Should().Be("Error1");
    }

    [Fact]
    public void ThrowIfInvalid_WithErrors_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = new ValidationResult { IsValid = false };
        result.Errors.Add("Error");

        // Act
        var act = () => result.ThrowIfInvalid("Custom message");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Custom message: Error");
    }

    [Fact]
    public void Log_ValidResult_LogsInformation()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };
        var mockLogger = new Mock<ILogger>();

        // Act
        result.Log(mockLogger.Object, "TestContext");

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration validation passed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
