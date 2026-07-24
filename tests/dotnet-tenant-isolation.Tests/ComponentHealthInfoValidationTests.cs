#nullable enable

using System;
using FluentAssertions;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class ComponentHealthInfoValidationTests
{
    [Fact]
    public void Validate_WithValidComponentHealthInfo_ReturnsEmptyList()
    {
        // Arrange
        var validComponent = new ComponentHealthInfo
        {
            Name = "database",
            Message = "Database is operational",
            ResponseTimeMs = 150,
            CheckedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var result = validComponent.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullComponent_ThrowsArgumentNullException()
    {
        // Arrange
        ComponentHealthInfo? nullComponent = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullComponent!.Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidName_ReturnsNameError(string invalidName)
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = invalidName,
            Message = "Test message",
            ResponseTimeMs = 100,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().ContainSingle(error => error.Contains("Name cannot be null, empty, or whitespace"));
    }

    [Fact]
    public void Validate_WithNullMessage_ReturnsMessageError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "cache",
            Message = null,
            ResponseTimeMs = 50,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().ContainSingle(error => error.Contains("Message cannot be null"));
    }

    [Fact]
    public void Validate_WithEmptyMessage_ReturnsNoError()
    {
        // Arrange
    var component = new ComponentHealthInfo
    {
        Name = "cache",
        Message = "",
        ResponseTimeMs = 50,
        CheckedAt = DateTime.UtcNow
    };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithWhitespaceMessage_ReturnsNoError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "cache",
            Message = "   ",
            ResponseTimeMs = 50,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNegativeResponseTime_ReturnsResponseTimeError(long negativeResponseTime)
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "database",
            Message = "Test message",
            ResponseTimeMs = negativeResponseTime,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().ContainSingle(error => error.Contains("ResponseTimeMs cannot be negative"));
    }

    [Fact]
    public void Validate_WithZeroResponseTime_ReturnsNoError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "cache",
            Message = "Test message",
            ResponseTimeMs = 0,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithPositiveResponseTime_ReturnsNoError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "database",
            Message = "Test message",
            ResponseTimeMs = 999,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithDefaultCheckedAt_ReturnsCheckedAtError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "cache",
            Message = "Test message",
            ResponseTimeMs = 100,
            CheckedAt = default
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().ContainSingle(error => error.Contains("CheckedAt cannot be default/MinValue"));
    }

    [Fact]
    public void Validate_WithRecentCheckedAt_ReturnsNoError()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "database",
            Message = "Test message",
            ResponseTimeMs = 150,
            CheckedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_WithValidComponent_ReturnsTrue()
    {
        // Arrange
        var validComponent = new ComponentHealthInfo
        {
            Name = "cache",
            Message = "Cache is operational",
            ResponseTimeMs = 25,
            CheckedAt = DateTime.UtcNow.AddSeconds(-30)
        };

        // Act
        var result = validComponent.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidComponent_ReturnsFalse()
    {
        // Arrange
        var invalidComponent = new ComponentHealthInfo
        {
            Name = "",
            Message = null,
            ResponseTimeMs = -50,
            CheckedAt = default
        };

        // Act
        var result = invalidComponent.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNullComponent_ThrowsArgumentNullException()
    {
        // Arrange
        ComponentHealthInfo? nullComponent = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullComponent!.IsValid());
    }

    [Fact]
    public void EnsureValid_WithValidComponent_DoesNotThrow()
    {
        // Arrange
        var validComponent = new ComponentHealthInfo
        {
            Name = "eventbus",
            Message = "Event bus operational",
            ResponseTimeMs = 10,
            CheckedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        // Act
        Action act = () => validComponent.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithInvalidComponent_ThrowsArgumentException()
    {
        // Arrange
        var invalidComponent = new ComponentHealthInfo
        {
            Name = null,
            Message = null,
            ResponseTimeMs = -100,
            CheckedAt = default
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => invalidComponent.EnsureValid());
        exception.Message.Should().Contain("ComponentHealthInfo is not valid");
        exception.Message.Should().Contain("Problems:");
    }

    [Fact]
    public void EnsureValid_WithNullComponent_ThrowsArgumentNullException()
    {
        // Arrange
        ComponentHealthInfo? nullComponent = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullComponent!.EnsureValid());
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var invalidComponent = new ComponentHealthInfo
        {
            Name = "   ",
            Message = null,
            ResponseTimeMs = -50,
            CheckedAt = default
        };

        // Act
        var result = invalidComponent.Validate();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(error => error.Contains("Name cannot be null, empty, or whitespace"));
        result.Should().Contain(error => error.Contains("Message cannot be null"));
        result.Should().Contain(error => error.Contains("ResponseTimeMs cannot be negative"));
        result.Should().Contain(error => error.Contains("CheckedAt cannot be default/MinValue"));
    }

    [Fact]
    public void Validate_WithAllValidProperties_ReturnsEmptyList()
    {
        // Arrange
        var component = new ComponentHealthInfo
        {
            Name = "database-connection",
            Status = HealthStatus.Degraded,
            Message = "Database responding slowly",
            ResponseTimeMs = 500,
            CheckedAt = DateTime.UtcNow
        };

        // Act
        var result = component.Validate();

        // Assert
        result.Should().BeEmpty();
    }
}