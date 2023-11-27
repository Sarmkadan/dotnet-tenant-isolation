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
public class DataIsolationPolicyTests
{
    /// <summary>
    /// Tests that GetAllowedFields returns a list with trimmed entries when given a comma-separated string.
    /// </summary>
    [Fact]
    public void GetAllowedFields_WithCommaSeparatedString_ReturnsListWithTrimmedEntries()
    {
        // Arrange
        var policy = new DataIsolationPolicy { AllowedFields = "Name, Email, Phone" };

        // Act
        var fields = policy.GetAllowedFields();

        // Assert
        fields.Should().HaveCount(3);
        fields.Should().ContainInOrder("Name", "Email", "Phone");
    }

    /// <summary>
    /// Tests that GetDeniedFields returns an empty list when the denied fields are null.
    /// </summary>
    [Fact]
    public void GetDeniedFields_WhenNull_ReturnsEmptyList()
    {
        // Arrange
        var policy = new DataIsolationPolicy { DeniedFields = null };

        // Act
        var fields = policy.GetDeniedFields();

        // Assert
        fields.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that IsFieldAccessAllowed returns false when the field is in the denied list.
    /// </summary>
    [Fact]
    public void IsFieldAccessAllowed_WhenFieldInDeniedList_ReturnsFalse()
    {
        // Arrange
        var policy = new DataIsolationPolicy { DeniedFields = "Password, SSN" };

        // Act & Assert
        policy.IsFieldAccessAllowed("Password").Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsFieldAccessAllowed returns false when the field is not in the allowed list.
    /// </summary>
    [Fact]
    public void IsFieldAccessAllowed_WhenFieldNotInAllowedList_ReturnsFalse()
    {
        // Arrange — explicit allow-list means anything not listed is denied
        var policy = new DataIsolationPolicy { AllowedFields = "Name, Email" };

        // Act & Assert
        policy.IsFieldAccessAllowed("Password").Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsFieldAccessAllowed returns true when there are no restrictions.
    /// </summary>
    [Fact]
    public void IsFieldAccessAllowed_WhenNoRestrictions_ReturnsTrue()
    {
        // Arrange — empty both lists = unrestricted access
        var policy = new DataIsolationPolicy
        {
            AllowedFields = null,
            DeniedFields = null
        };

        // Act & Assert
        policy.IsFieldAccessAllowed("AnyField").Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsFieldAccessAllowed is case-insensitive.
    /// </summary>
    [Fact]
    public void IsFieldAccessAllowed_IsCaseInsensitive()
    {
        // Arrange
        var policy = new DataIsolationPolicy { DeniedFields = "Password" };

        // Act & Assert
        policy.IsFieldAccessAllowed("password").Should().BeFalse();
        policy.IsFieldAccessAllowed("PASSWORD").Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsCrossTenantAccessAllowed always returns false for a strict policy.
    /// </summary>
    [Fact]
    public void IsCrossTenantAccessAllowed_WhenStrictPolicy_AlwaysReturnsFalse()
    {
        // Arrange — strict policy never permits cross-tenant reads, even if tenant ID is listed
        var otherTenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            PolicyType = DataIsolationPolicyType.Strict,
            AllowedCrossTenantAccess = otherTenantId.ToString()
        };

        // Act & Assert
        policy.IsCrossTenantAccessAllowed(otherTenantId).Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsCrossTenantAccessAllowed returns true for a relaxed policy when the tenant ID is in the allowed list.
    /// </summary>
    [Fact]
    public void IsCrossTenantAccessAllowed_WhenRelaxedAndTenantInAllowedList_ReturnsTrue()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            PolicyType = DataIsolationPolicyType.Relaxed,
            AllowedCrossTenantAccess = otherTenantId.ToString()
        };

        // Act & Assert
        policy.IsCrossTenantAccessAllowed(otherTenantId).Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsValidPolicy returns false with a message when a custom policy has no filter rule.
    /// </summary>
    [Fact]
    public void IsValidPolicy_WhenCustomPolicyHasNoFilterRule_ReturnsFalseWithMessage()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            EntityType = "Order",
            PolicyType = DataIsolationPolicyType.Custom,
            FilterRule = null
        };

        // Act
        var isValid = policy.IsValidPolicy(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("Filter rule");
    }

    /// <summary>
    /// Tests that IsValidPolicy returns false with a message when the same field is in both the allowed and denied lists.
    /// </summary>
    [Fact]
    public void IsValidPolicy_WhenSameFieldInAllowedAndDenied_ReturnsFalseWithOverlapMessage()
    {
        // Arrange — overlapping lists indicate a misconfigured policy
        var policy = new DataIsolationPolicy
        {
            EntityType = "Customer",
            PolicyType = DataIsolationPolicyType.Relaxed,
            AllowedFields = "Email, Name",
            DeniedFields = "Email, SSN"
        };

        // Act
        var isValid = policy.IsValidPolicy(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("Email");
    }

    /// <summary>
    /// Tests that IsValidPolicy returns false with a message when the entity type is empty.
    /// </summary>
    [Fact]
    public void IsValidPolicy_WhenEntityTypeIsEmpty_ReturnsFalseWithMessage()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            EntityType = "",
            PolicyType = DataIsolationPolicyType.Strict
        };

        // Act
        var isValid = policy.IsValidPolicy(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("Entity type");
    }
}
