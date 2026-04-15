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

public class TenantModelTests
{
    [Fact]
    public void CanActivate_WhenActiveAndNotDeleted_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Status = TenantStatus.Active,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanActivate_WhenDeleted_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = true,
            Status = TenantStatus.Active
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanActivate_WhenStatusIsArchived_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = false,
            Status = TenantStatus.Archived
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanActivate_WhenSubscriptionExpired_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = false,
            Status = TenantStatus.Active,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanActivate_WhenSubscriptionExpiryIsNull_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = false,
            Status = TenantStatus.Active,
            SubscriptionExpiresAt = null
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsUserLimitExceeded_WhenCurrentUsageAtExactLimit_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant { MaxUsers = 50 };

        // Act
        var result = tenant.IsUserLimitExceeded(50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsUserLimitExceeded_WhenCurrentUsageOneBelowLimit_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant { MaxUsers = 50 };

        // Act
        var result = tenant.IsUserLimitExceeded(49);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUserLimitExceeded_WhenMaxUsersIsNull_ReturnsFalse()
    {
        // Arrange — unlimited tenant allows any user count
        var tenant = new Tenant { MaxUsers = null };

        // Act
        var result = tenant.IsUserLimitExceeded(int.MaxValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSubscriptionValid_WhenExpiryIsNull_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant { SubscriptionExpiresAt = null };

        // Act & Assert
        tenant.IsSubscriptionValid().Should().BeTrue();
    }

    [Fact]
    public void IsSubscriptionValid_WhenExpiredYesterday_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant { SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1) };

        // Act & Assert
        tenant.IsSubscriptionValid().Should().BeFalse();
    }

    [Fact]
    public void Delete_SetsIsDeletedTrueAndArchivesStatus()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = false,
            Status = TenantStatus.Active
        };

        // Act
        tenant.Delete();

        // Assert
        tenant.IsDeleted.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Archived);
        tenant.DeletedAt.Should().NotBeNull();
        tenant.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Restore_ClearsDeletedFlagAndSetsStatusToActive()
    {
        // Arrange
        var tenant = new Tenant
        {
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-7),
            Status = TenantStatus.Archived
        };

        // Act
        tenant.Restore();

        // Assert
        tenant.IsDeleted.Should().BeFalse();
        tenant.DeletedAt.Should().BeNull();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Suspend_TransitionsStatusToSuspended()
    {
        // Arrange
        var tenant = new Tenant { Status = TenantStatus.Active };

        // Act
        tenant.Suspend("Payment overdue");

        // Assert
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void IsInTrial_WhenStatusIsTrial_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant { Status = TenantStatus.Trial };

        // Act & Assert
        tenant.IsInTrial().Should().BeTrue();
    }

    [Fact]
    public void IsInTrial_WhenStatusIsActive_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant { Status = TenantStatus.Active };

        // Act & Assert
        tenant.IsInTrial().Should().BeFalse();
    }
}
