using System;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantConfigurationExtensionsTests
{
    [Fact]
    public void IsValidForValueType_WithMatchingType_ReturnsTrue()
    {
        // Arrange
        var config = new TenantConfiguration();
        config.SetValue(42); // int value, should set ValueType to "Int32"

        // Act
        var result = config.IsValidForValueType<int>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidForValueType_WithMismatchingType_ReturnsFalse()
    {
        // Arrange
        var config = new TenantConfiguration();
        config.SetValue("hello"); // string value, ValueType = "String"

        // Act
        var result = config.IsValidForValueType<int>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidForValueType_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        TenantConfiguration? config = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config!.IsValidForValueType<int>());
    }

    [Fact]
    public void TryGetValueAs_WithValidValue_ReturnsTrueAndCorrectValue()
    {
        // Arrange
        var config = new TenantConfiguration();
        config.SetValue(123);

        // Act
        var success = config.TryGetValueAs<int>(out var value);

        // Assert
        Assert.True(success);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueAs_WithInvalidFormat_ReturnsFalse()
    {
        // Arrange
        var config = new TenantConfiguration();
        // Store a string that cannot be converted to int
        config.SetValue("not-an-int");

        // Act
        var success = config.TryGetValueAs<int>(out var value);

        // Assert
        Assert.False(success);
        Assert.Equal(default(int), value);
    }

    [Fact]
    public void TryGetValueAs_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        TenantConfiguration? config = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config!.TryGetValueAs<int>(out _));
    }

    [Fact]
    public void UpdateValue_UpdatesValueAndModifiedAt()
    {
        // Arrange
        var config = new TenantConfiguration();
        config.SetValue("old");
        var originalModified = DateTime.UtcNow.AddMinutes(-5);
        config.ModifiedAt = originalModified;

        // Act
        config.UpdateValue("new");

        // Assert
        Assert.True(config.ModifiedAt > originalModified, "ModifiedAt should be updated to a later time");
        var retrieved = config.GetValueAs<string>();
        Assert.Equal("new", retrieved);
    }

    [Fact]
    public void UpdateValue_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        TenantConfiguration? config = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config!.UpdateValue("value"));
    }
}
