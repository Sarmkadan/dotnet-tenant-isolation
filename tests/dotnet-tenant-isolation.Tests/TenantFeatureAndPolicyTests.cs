#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using TenantIsolation.Constants;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Tests for the TenantFeature class.
/// </summary>
public class TenantFeatureTests
{
    /// <summary>
    /// Tests that IsAvailable returns false when the feature is disabled.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = false, RolloutPercentage = 100 };

        // Act & Assert
        feature.IsAvailable().Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsAvailable returns false when the feature is deprecated before the current time.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenDeprecatedBeforeNow_ReturnsFalse()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100,
            DeprecatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        feature.IsAvailable().Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsAvailable returns false when the feature's available from date is in the future.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenAvailableFromIsInFuture_ReturnsFalse()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100,
            AvailableFrom = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        feature.IsAvailable().Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsAvailable returns true when the feature is enabled with a full rollout.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenEnabledWithFullRollout_ReturnsTrue()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100
        };

        // Act & Assert
        feature.IsAvailable().Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsUsageLimitExceeded returns true when the current usage equals the limit.
    /// </summary>
    [Fact]
    public void IsUsageLimitExceeded_WhenCurrentUsageEqualsLimit_ReturnsTrue()
    {
        // Arrange
        var feature = new TenantFeature { UsageLimit = 100, CurrentUsage = 100 };

        // Act & Assert
        feature.IsUsageLimitExceeded().Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsUsageLimitExceeded returns false when the usage limit is null.
    /// </summary>
    [Fact]
    public void IsUsageLimitExceeded_WhenUsageLimitIsNull_ReturnsFalse()
    {
        // Arrange — null limit means unlimited usage
        var feature = new TenantFeature { UsageLimit = null, CurrentUsage = long.MaxValue };

        // Act & Assert
        feature.IsUsageLimitExceeded().Should().BeFalse();
    }

    /// <summary>
    /// Tests that CanUseFeature returns false with a deprecation message when the feature is deprecated.
    /// </summary>
    [Fact]
    public void CanUseFeature_WhenDeprecated_ReturnsFalseWithDeprecationMessage()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100,
            DeprecatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var canUse = feature.CanUseFeature(out var error);

        // Assert
        canUse.Should().BeFalse();
        error.Should().Contain("deprecated");
    }

    /// <summary>
    /// Tests that CanUseFeature returns false with a limit message when the usage limit is reached.
    /// </summary>
    [Fact]
    public void CanUseFeature_WhenUsageLimitReached_ReturnsFalseWithLimitMessage()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100,
            UsageLimit = 10,
            CurrentUsage = 10
        };

        // Act
        var canUse = feature.CanUseFeature(out var error);

        // Assert
        canUse.Should().BeFalse();
        error.Should().Contain("10");
    }

    /// <summary>
    /// Tests that CanUseFeature returns true with a null error when the feature is available and within limits.
    /// </summary>
    [Fact]
    public void CanUseFeature_WhenAvailableAndWithinLimits_ReturnsTrueWithNullError()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            RolloutPercentage = 100,
            UsageLimit = 100,
            CurrentUsage = 50
        };

        // Act
        var canUse = feature.CanUseFeature(out var error);

        // Assert
        canUse.Should().BeTrue();
        error.Should().BeNull();
    }

    /// <summary>
    /// Tests that RecordUsage increments the current usage by the specified amount.
    /// </summary>
    [Fact]
    public void RecordUsage_IncrementsCurrentUsageBySpecifiedAmount()
    {
        // Arrange
        var feature = new TenantFeature { CurrentUsage = 5 };

        // Act
        feature.RecordUsage(3);

        // Assert
        feature.CurrentUsage.Should().Be(8);
    }

    /// <summary>
    /// Tests that ResetUsage sets the current usage back to zero.
    /// </summary>
    [Fact]
    public void ResetUsage_SetsCurrentUsageBackToZero()
    {
        // Arrange
        var feature = new TenantFeature { CurrentUsage = 999 };

        // Act
        feature.ResetUsage();

        // Assert
        feature.CurrentUsage.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetStatus returns "Deprecated" when the feature is deprecated in the past.
    /// </summary>
    [Fact]
    public void GetStatus_WhenDeprecatedInPast_ReturnsDeprecated()
    {
        // Arrange
        var feature = new TenantFeature
        {
            IsEnabled = true,
            DeprecatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        feature.GetStatus().Should().Be("Deprecated");
    }

    /// <summary>
    /// Tests that GetStatus returns "Disabled" when the feature is disabled.
    /// </summary>
    [Fact]
    public void GetStatus_WhenDisabled_ReturnsDisabled()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = false };

        // Act & Assert
        feature.GetStatus().Should().Be("Disabled");
    }

    /// <summary>
    /// Tests that GetStatus returns "Beta (X%)" when the feature has a partial rollout.
    /// </summary>
    [Fact]
    public void GetStatus_WhenPartialRollout_ReturnsBetaWithPercentage()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = true, RolloutPercentage = 50 };

        // Act & Assert
        feature.GetStatus().Should().Be("Beta (50%)");
    }

    /// <summary>
    /// Tests that GetStatus returns "Active" when the feature is fully active.
    /// </summary>
    [Fact]
    public void GetStatus_WhenFullyActiveFeature_ReturnsActive()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = true, RolloutPercentage = 100 };

        // Act & Assert
        feature.GetStatus().Should().Be("Active");
    }
}

/// <summary>
/// Tests for the DataIsolationPolicy class.
/// </summary>
