#nullable enable

using System;
using Xunit;
using TenantIsolation.Services;

namespace TenantIsolation.Tests;

public class ComponentHealthInfoTests
{
    [Fact]
    public void Constructor_WithDefaultValues_CreatesHealthyComponent()
    {
        // Arrange & Act
        var component = new ComponentHealthInfo();

        // Assert
        Assert.Equal(string.Empty, component.Name);
        Assert.Equal(HealthStatus.Healthy, component.Status);
        Assert.Equal(string.Empty, component.Message);
        Assert.Equal(0, component.ResponseTimeMs);
        Assert.True(component.CheckedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsPropertiesCorrectly()
    {
        // Arrange
        var name = "database";
        var status = HealthStatus.Degraded;
        var message = "Slow response time";
        var responseTimeMs = 500L;
        var checkedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var component = new ComponentHealthInfo
        {
            Name = name,
            Status = status,
            Message = message,
            ResponseTimeMs = responseTimeMs,
            CheckedAt = checkedAt
        };

        // Assert
        Assert.Equal(name, component.Name);
        Assert.Equal(status, component.Status);
        Assert.Equal(message, component.Message);
        Assert.Equal(responseTimeMs, component.ResponseTimeMs);
        Assert.Equal(checkedAt, component.CheckedAt);
    }

    [Fact]
    public void ResponseTimeMs_WhenSetToZero_IsValid()
    {
        // Arrange & Act
        var component = new ComponentHealthInfo { ResponseTimeMs = 0 };

        // Assert
        Assert.Equal(0, component.ResponseTimeMs);
    }

    [Fact]
    public void Status_WhenChanged_UpdatesCorrectly()
    {
        // Arrange
        var component = new ComponentHealthInfo { Status = HealthStatus.Healthy };

        // Act
        component.Status = HealthStatus.Degraded;

        // Assert
        Assert.Equal(HealthStatus.Degraded, component.Status);

        // Act
        component.Status = HealthStatus.Unhealthy;

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, component.Status);

        // Act
        component.Status = HealthStatus.Healthy;

        // Assert
        Assert.Equal(HealthStatus.Healthy, component.Status);
    }
}