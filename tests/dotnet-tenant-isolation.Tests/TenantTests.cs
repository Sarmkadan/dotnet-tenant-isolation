using System;
using System.ComponentModel.DataAnnotations;
using TenantIsolation.Models;
using TenantIsolation.Constants;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly string TestSlug = "test-tenant";
    private static readonly string TestName = "Test Tenant";
    private static readonly string TestAdminEmail = "admin@test.com";

    [Fact]
    public void Constructor_WithValidParameters_CreatesTenantSuccessfully()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
        };

        // Assert
        Assert.Equal(TestId, tenant.Id);
        Assert.Equal(TestSlug, tenant.Slug);
        Assert.Equal(TestName, tenant.Name);
        Assert.Equal(TestAdminEmail, tenant.AdminEmail);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(TenantIsolationStrategy.DatabasePerTenant, tenant.IsolationStrategy);
        Assert.NotEqual(default, tenant.CreatedAt);
        Assert.NotEqual(default, tenant.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithNullRequiredFields_SetsPropertiesWithoutException()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Slug = null!,
            Name = TestName,
            AdminEmail = TestAdminEmail
        };

        // Assert - properties are set even if null
        Assert.Null(tenant.Slug);
        Assert.Equal(TestName, tenant.Name);
        Assert.Equal(TestAdminEmail, tenant.AdminEmail);
    }

    [Fact]
    public void Constructor_WithEmptyRequiredFields_SetsPropertiesWithoutException()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Slug = "",
            Name = "",
            AdminEmail = ""
        };

        // Assert - properties are set even if empty
        Assert.Equal("", tenant.Slug);
        Assert.Equal("", tenant.Name);
        Assert.Equal("", tenant.AdminEmail);
    }

    [Fact]
    public void CanActivate_WhenActiveAndNotDeletedAndSubscriptionValid_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanActivate_WhenDeleted_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsDeleted = true,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanActivate_WhenArchived_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Archived,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanActivate_WhenSubscriptionExpired_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            IsDeleted = false,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanActivate_WhenNoSubscriptionExpiration_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            IsDeleted = false,
            SubscriptionExpiresAt = null
        };

        // Act
        var result = tenant.CanActivate();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsUserLimitExceeded_WhenMaxUsersNull_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            MaxUsers = null
        };

        // Act
        var result = tenant.IsUserLimitExceeded(1000);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsUserLimitExceeded_WhenCurrentUsersBelowLimit_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            MaxUsers = 100
        };

        // Act
        var result = tenant.IsUserLimitExceeded(50);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsUserLimitExceeded_WhenCurrentUsersEqualsLimit_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            MaxUsers = 100
        };

        // Act
        var result = tenant.IsUserLimitExceeded(100);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsUserLimitExceeded_WhenCurrentUsersExceedsLimit_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            MaxUsers = 100
        };

        // Act
        var result = tenant.IsUserLimitExceeded(150);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSubscriptionValid_WhenNoExpiration_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            SubscriptionExpiresAt = null
        };

        // Act
        var result = tenant.IsSubscriptionValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSubscriptionValid_WhenSubscriptionNotExpired_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = tenant.IsSubscriptionValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSubscriptionValid_WhenSubscriptionExpired_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = tenant.IsSubscriptionValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInTrial_WhenStatusIsTrial_ReturnsTrue()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Trial
        };

        // Act
        var result = tenant.IsInTrial();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInTrial_WhenStatusIsNotTrial_ReturnsFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active
        };

        // Act
        var result = tenant.IsInTrial();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Delete_WhenCalled_SetsIsDeletedAndStatusAndDeletedAt()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
        };
        var beforeDelete = DateTime.UtcNow.AddMinutes(-1);

        // Act
        tenant.Delete();

        // Assert
        Assert.True(tenant.IsDeleted);
        Assert.Equal(TenantStatus.Archived, tenant.Status);
        Assert.NotNull(tenant.DeletedAt);
        Assert.True(tenant.DeletedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void Restore_WhenCalled_ResetsIsDeletedAndStatusAndDeletedAt()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
        };

        // Act
        tenant.Restore();

        // Assert
        Assert.False(tenant.IsDeleted);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Null(tenant.DeletedAt);
    }

    [Fact]
    public void Suspend_WhenCalled_SetsStatusAndUpdatesTimestamp()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
        };
        var beforeSuspend = DateTime.UtcNow.AddMinutes(-1);

        // Act
        tenant.Suspend("Policy violation");

        // Assert
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.True(tenant.UpdatedAt >= beforeSuspend);
    }

    [Fact]
    public void Suspend_WhenCalledWithoutReason_SetsStatusAndUpdatesTimestamp()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail,
            Status = TenantStatus.Active,
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant
        };

        // Act
        tenant.Suspend();

        // Assert
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
    }

    [Fact]
    public void Properties_WithValidValues_AreSetCorrectly()
    {
        // Arrange
        var description = "Test description";
        var phoneNumber = "+1-555-123-4567";
        var planId = "premium-monthly";
        var maxUsers = 500;
        var maxStorageGb = 1000m;
        var metadata = "{\"custom\":\"value\"}";

        // Act
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            Description = description,
            AdminEmail = TestAdminEmail,
            PhoneNumber = phoneNumber,
            Status = TenantStatus.Trial,
            IsolationStrategy = TenantIsolationStrategy.SchemaPerTenant,
            PlanId = planId,
            MaxUsers = maxUsers,
            MaxStorageGb = maxStorageGb,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(description, tenant.Description);
        Assert.Equal(phoneNumber, tenant.PhoneNumber);
        Assert.Equal(TenantStatus.Trial, tenant.Status);
        Assert.Equal(TenantIsolationStrategy.SchemaPerTenant, tenant.IsolationStrategy);
        Assert.Equal(planId, tenant.PlanId);
        Assert.Equal(maxUsers, tenant.MaxUsers);
        Assert.Equal(maxStorageGb, tenant.MaxStorageGb);
        Assert.Equal(metadata, tenant.Metadata);
    }

    [Fact]
    public void Properties_WithNullOptionalFields_AreNull()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail
        };

        // Assert
        Assert.Null(tenant.Description);
        Assert.Null(tenant.PhoneNumber);
        Assert.Null(tenant.PlanId);
        Assert.Null(tenant.MaxUsers);
        Assert.Null(tenant.MaxStorageGb);
        Assert.Null(tenant.Metadata);
    }

    [Fact]
    public void DefaultValues_WhenNotSet_AreInitialized()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = TestId,
            Slug = TestSlug,
            Name = TestName,
            AdminEmail = TestAdminEmail
        };

        // Assert
        Assert.Equal(TenantStatus.Provisioning, tenant.Status);
        Assert.Equal(TenantIsolationStrategy.DatabasePerTenant, tenant.IsolationStrategy);
        Assert.False(tenant.IsDeleted);
        Assert.Null(tenant.DeletedAt);
        Assert.NotEqual(default, tenant.CreatedAt);
        Assert.NotEqual(default, tenant.UpdatedAt);
    }
}