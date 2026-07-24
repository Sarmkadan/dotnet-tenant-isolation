using Xunit;
using System;
using System.Collections.Generic;

namespace tests.dotnet_tenant_isolation.Tests;

public class TenantConfigurationTests
{
    [Fact]
    public void GetValueAs_HappyPath_ReturnsCorrectValue()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = "testKey",
            Value = "testValue",
            ValueType = "string"
        };

        // Act
        var result = tenantConfiguration.GetValueAs<string>();

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetValueAs_NullValue_ReturnsNull()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = "testKey",
            Value = null,
            ValueType = "string"
        };

        // Act
        var result = tenantConfiguration.GetValueAs<string>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetValue_HappyPath_SetsCorrectValue()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = "testKey",
            Value = "oldValue",
            ValueType = "string"
        };

        // Act
        tenantConfiguration.SetValue("newValue");

        // Assert
        Assert.Equal("newValue", tenantConfiguration.Value);
    }

    [Fact]
    public void IsValid_HappyPath_ReturnsTrue()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = "testKey",
            Value = "testValue",
            ValueType = "string"
        };

        // Act
        var result = tenantConfiguration.IsValid(out _);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_NullKey_ReturnsFalse()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = null,
            Value = "testValue",
            ValueType = "string"
        };

        // Act
        var result = tenantConfiguration.IsValid(out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_RequiredButEmptyValue_ReturnsFalse()
    {
        // Arrange
        var tenantConfiguration = new TenantIsolation.Models.TenantConfiguration
        {
            Key = "testKey",
            Value = "",
            ValueType = "string",
            IsRequired = true
        };

        // Act
        var result = tenantConfiguration.IsValid(out _);

        // Assert
        Assert.False(result);
    }
}
