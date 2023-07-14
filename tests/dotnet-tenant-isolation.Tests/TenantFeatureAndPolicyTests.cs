// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using TenantIsolation.Constants;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantFeatureTests
{
    [Fact]
    public void IsAvailable_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = false, RolloutPercentage = 100 };

        // Act & Assert
        feature.IsAvailable().Should().BeFalse();
    }

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

    [Fact]
    public void IsUsageLimitExceeded_WhenCurrentUsageEqualsLimit_ReturnsTrue()
    {
        // Arrange
        var feature = new TenantFeature { UsageLimit = 100, CurrentUsage = 100 };

        // Act & Assert
        feature.IsUsageLimitExceeded().Should().BeTrue();
    }

    [Fact]
    public void IsUsageLimitExceeded_WhenUsageLimitIsNull_ReturnsFalse()
    {
        // Arrange — null limit means unlimited usage
        var feature = new TenantFeature { UsageLimit = null, CurrentUsage = long.MaxValue };

        // Act & Assert
        feature.IsUsageLimitExceeded().Should().BeFalse();
    }

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

    [Fact]
    public void GetStatus_WhenDisabled_ReturnsDisabled()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = false };

        // Act & Assert
        feature.GetStatus().Should().Be("Disabled");
    }

    [Fact]
    public void GetStatus_WhenPartialRollout_ReturnsBetaWithPercentage()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = true, RolloutPercentage = 50 };

        // Act & Assert
        feature.GetStatus().Should().Be("Beta (50%)");
    }

    [Fact]
    public void GetStatus_WhenFullyActiveFeature_ReturnsActive()
    {
        // Arrange
        var feature = new TenantFeature { IsEnabled = true, RolloutPercentage = 100 };

        // Act & Assert
        feature.GetStatus().Should().Be("Active");
    }
}

public class DataIsolationPolicyTests
{
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

    [Fact]
    public void IsFieldAccessAllowed_WhenFieldInDeniedList_ReturnsFalse()
    {
        // Arrange
        var policy = new DataIsolationPolicy { DeniedFields = "Password, SSN" };

        // Act & Assert
        policy.IsFieldAccessAllowed("Password").Should().BeFalse();
    }

    [Fact]
    public void IsFieldAccessAllowed_WhenFieldNotInAllowedList_ReturnsFalse()
    {
        // Arrange — explicit allow-list means anything not listed is denied
        var policy = new DataIsolationPolicy { AllowedFields = "Name, Email" };

        // Act & Assert
        policy.IsFieldAccessAllowed("Password").Should().BeFalse();
    }

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

    [Fact]
    public void IsFieldAccessAllowed_IsCaseInsensitive()
    {
        // Arrange
        var policy = new DataIsolationPolicy { DeniedFields = "Password" };

        // Act & Assert
        policy.IsFieldAccessAllowed("password").Should().BeFalse();
        policy.IsFieldAccessAllowed("PASSWORD").Should().BeFalse();
    }

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
