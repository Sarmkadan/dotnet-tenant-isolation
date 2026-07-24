#nullable enable

using System;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public class UserTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestOrganizationId = Guid.NewGuid();
    private static readonly string TestEmail = "test@example.com";
    private static readonly string TestFirstName = "John";
    private static readonly string TestLastName = "Doe";
    private static readonly string TestRole = "Admin";

    [Fact]
    public void Constructor_WithValidParameters_CreatesUserSuccessfully()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole
        };

        // Assert
        Assert.Equal(TestEmail, user.Email);
        Assert.Equal(TestFirstName, user.FirstName);
        Assert.Equal(TestLastName, user.LastName);
        Assert.Equal(TestRole, user.Role);
        Assert.Equal(TestTenantId, user.TenantId);
        Assert.Equal(TestOrganizationId, user.OrganizationId);
        Assert.True(user.IsActive); // Default value
        Assert.False(user.IsEmailVerified); // Default value
        Assert.Equal(DateTime.UtcNow.Date, user.CreatedAt.Date);
        Assert.Equal(DateTime.UtcNow.Date, user.UpdatedAt.Date);
    }

    [Fact]
    public void GetFullName_WithValidNames_ReturnsFullName()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole
        };

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal($"{TestFirstName} {TestLastName}", fullName);
    }

    [Fact]
    public void GetFullName_WithExtraSpaces_ReturnsTrimmedFullName()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = "John",
            LastName = "Doe",
            Role = "User"
        };

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void GetFullName_WithEmptyLastName_ReturnsFirstNameOnly()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = "",
            Role = TestRole
        };

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal(TestFirstName, fullName);
    }

    [Fact]
    public void IsAccountLocked_WhenNotLocked_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            LockedUntil = null
        };

        // Act
        var isLocked = user.IsAccountLocked();

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public void IsAccountLocked_WhenLockedInFuture_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            LockedUntil = DateTime.UtcNow.AddMinutes(30),
            FailedLoginAttempts = 5
        };

        // Act
        var isLocked = user.IsAccountLocked();

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public void IsAccountLocked_WhenLockedInPast_ResetsAndReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            LockedUntil = DateTime.UtcNow.AddMinutes(-30), // Locked in the past
            FailedLoginAttempts = 5
        };

        // Act
        var isLocked = user.IsAccountLocked();

        // Assert
        Assert.False(isLocked); // Should be reset
        Assert.Null(user.LockedUntil); // Should be cleared
        Assert.Equal(0, user.FailedLoginAttempts); // Should be reset
    }

    [Fact]
    public void RecordFailedLoginAttempt_IncrementsCounter()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            FailedLoginAttempts = 0
        };

        // Act
        user.RecordFailedLoginAttempt();

        // Assert
        Assert.Equal(1, user.FailedLoginAttempts);
        Assert.NotEqual(default, user.UpdatedAt);
    }

    [Fact]
    public void RecordFailedLoginAttempt_WhenReachesMax_LocksAccount()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            FailedLoginAttempts = 4
        };

        // Act
        user.RecordFailedLoginAttempt(maxAttempts: 5);

        // Assert
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.NotNull(user.LockedUntil);
        Assert.True(user.LockedUntil > DateTime.UtcNow);
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedAttemptsAndUpdatesLogin()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            FailedLoginAttempts = 3,
            LastLoginAt = DateTime.UtcNow.AddHours(-2)
        };

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntil);
        Assert.True(user.LastLoginAt >= DateTime.UtcNow.AddSeconds(-1));
        Assert.NotEqual(default, user.UpdatedAt);
    }

    [Fact]
    public void SetPasswordHashAndReset_UpdatesPasswordHashAndTimestamps()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            FailedLoginAttempts = 2,
            LockedUntil = DateTime.UtcNow.AddMinutes(10)
        };
        var passwordHash = "hashed_password_123";

        // Act
        user.SetPasswordHashAndReset(passwordHash);

        // Assert
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntil);
        Assert.NotNull(user.LastPasswordChangeAt);
        Assert.True(user.LastPasswordChangeAt <= DateTime.UtcNow);
        Assert.NotEqual(default, user.UpdatedAt);
    }

    [Fact]
    public void IsPasswordChangeRequired_WhenNeverChanged_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole
        };

        // Act
        var requiresChange = user.IsPasswordChangeRequired();

        // Assert
        Assert.True(requiresChange);
    }

    [Fact]
    public void IsPasswordChangeRequired_WhenWithinMaxAge_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            LastPasswordChangeAt = DateTime.UtcNow.AddDays(-45)
        };

        // Act
        var requiresChange = user.IsPasswordChangeRequired(maxPasswordAgeDays: 90);

        // Assert
        Assert.False(requiresChange);
    }

    [Fact]
    public void IsPasswordChangeRequired_WhenExceedsMaxAge_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            LastPasswordChangeAt = DateTime.UtcNow.AddDays(-91)
        };

        // Act
        var requiresChange = user.IsPasswordChangeRequired(maxPasswordAgeDays: 90);

        // Assert
        Assert.True(requiresChange);
    }

    [Fact]
    public void CanLogin_WhenActiveAndNotDeletedAndNotLockedAndEmailVerified_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = true,
            IsDeleted = false,
            IsEmailVerified = true,
            LockedUntil = null
        };

        // Act
        var canLogin = user.CanLogin(out var errorMessage);

        // Assert
        Assert.True(canLogin);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanLogin_WhenInactive_ReturnsFalseWithError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = false,
            IsDeleted = false,
            IsEmailVerified = true,
            LockedUntil = null
        };

        // Act
        var canLogin = user.CanLogin(out var errorMessage);

        // Assert
        Assert.False(canLogin);
        Assert.Equal("User account is disabled", errorMessage);
    }

    [Fact]
    public void CanLogin_WhenDeleted_ReturnsFalseWithError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = true,
            IsDeleted = true,
            IsEmailVerified = true,
            LockedUntil = null
        };

        // Act
        var canLogin = user.CanLogin(out var errorMessage);

        // Assert
        Assert.False(canLogin);
        Assert.Equal("User account has been deleted", errorMessage);
    }

    [Fact]
    public void CanLogin_WhenLocked_ReturnsFalseWithError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = true,
            IsDeleted = false,
            IsEmailVerified = true,
            LockedUntil = DateTime.UtcNow.AddMinutes(30)
        };

        // Act
        var canLogin = user.CanLogin(out var errorMessage);

        // Assert
        Assert.False(canLogin);
        Assert.StartsWith("Account is locked until", errorMessage);
    }

    [Fact]
    public void CanLogin_WhenEmailNotVerified_ReturnsFalseWithError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = true,
            IsDeleted = false,
            IsEmailVerified = false,
            LockedUntil = null
        };

        // Act
        var canLogin = user.CanLogin(out var errorMessage);

        // Assert
        Assert.False(canLogin);
        Assert.Equal("Email address must be verified", errorMessage);
    }

    [Fact]
    public void Delete_WhenCalled_SetsIsDeletedAndIsActiveAndUpdatesTimestamp()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole,
            IsActive = true,
            IsDeleted = false
        };
        var beforeDelete = DateTime.UtcNow.AddMinutes(-1);

        // Act
        user.Delete();

        // Assert
        Assert.True(user.IsDeleted);
        Assert.False(user.IsActive);
        Assert.True(user.UpdatedAt >= beforeDelete);
    }

    [Fact]
    public void Properties_WithValidValues_AreSetCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var email = "user@company.com";
        var firstName = "Jane";
        var lastName = "Smith";
        var role = "Manager";
        var passwordHash = "secure_hash_value";
        var phoneNumber = "+1-555-987-6543";
        var avatarUrl = "https://example.com/avatar.jpg";
        var preferences = "{\"theme\":\"dark\"}";

        // Act
        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            OrganizationId = organizationId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            AvatarUrl = avatarUrl,
            Preferences = preferences,
            IsActive = false,
            IsEmailVerified = true,
            IsTwoFactorEnabled = true,
            FailedLoginAttempts = 2,
            LockedUntil = DateTime.UtcNow.AddMinutes(15)
        };

        // Assert
        Assert.Equal(userId, user.Id);
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal(organizationId, user.OrganizationId);
        Assert.Equal(email, user.Email);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(role, user.Role);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(phoneNumber, user.PhoneNumber);
        Assert.Equal(avatarUrl, user.AvatarUrl);
        Assert.Equal(preferences, user.Preferences);
        Assert.False(user.IsActive);
        Assert.True(user.IsEmailVerified);
        Assert.True(user.IsTwoFactorEnabled);
        Assert.Equal(2, user.FailedLoginAttempts);
        Assert.NotNull(user.LockedUntil);
    }

    [Fact]
    public void Properties_WithNullOptionalFields_AreNull()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = TestRole
        };

        // Assert
        Assert.Null(user.PasswordHash);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.AvatarUrl);
        Assert.Null(user.Preferences);
    }

    [Fact]
    public void DefaultValues_WhenNotSet_AreInitialized()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TestTenantId,
            OrganizationId = TestOrganizationId,
            Email = TestEmail,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Role = "User"
        };

        // Assert
        Assert.Equal("User", user.Role); // Default role
        Assert.True(user.IsActive); // Default value
        Assert.False(user.IsEmailVerified); // Default value
        Assert.False(user.IsTwoFactorEnabled); // Default value
        Assert.Equal(0, user.FailedLoginAttempts); // Default value
        Assert.Null(user.LockedUntil); // Default value
        Assert.Null(user.LastLoginAt); // Default value
        Assert.Null(user.LastPasswordChangeAt); // Default value
        Assert.False(user.IsDeleted); // Default value
        Assert.Equal(DateTime.UtcNow.Date, user.CreatedAt.Date);
        Assert.Equal(DateTime.UtcNow.Date, user.UpdatedAt.Date);
    }
}