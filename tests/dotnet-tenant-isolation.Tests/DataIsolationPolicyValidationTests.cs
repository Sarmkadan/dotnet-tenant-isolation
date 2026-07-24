#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using TenantIsolation.Constants;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Unit tests for the <see cref="DataIsolationPolicyValidation"/> static class.
/// Tests all public validation methods: Validate, IsValid, and EnsureValid.
/// </summary>
public class DataIsolationPolicyValidationTests
{
    private static DataIsolationPolicy CreateValidPolicy()
    {
        return new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            EntityType = "Order",
            PolicyType = DataIsolationPolicyType.Strict,
            Priority = 100,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
    }

    [Fact]
    public void Validate_WithNullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        DataIsolationPolicy? policy = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy!.Validate());
    }

    [Fact]
    public void Validate_WithValidStrictPolicy_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidCustomPolicy_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = DataIsolationPolicyType.Custom;
        policy.FilterRule = "WHERE TenantId = @tenantId";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidRelaxedPolicy_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = DataIsolationPolicyType.Relaxed;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.Id = Guid.Empty;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "Id must be a non-empty GUID");
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.TenantId = Guid.Empty;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "TenantId must be a non-empty GUID");
    }

    [Fact]
    public void Validate_WithNullEntityType_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = null!;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "EntityType is required");
    }

    [Fact]
    public void Validate_WithEmptyEntityType_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = "";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "EntityType is required");
    }

    [Fact]
    public void Validate_WithWhitespaceEntityType_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = "   ";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "EntityType is required");
    }

    [Fact]
    public void Validate_WithLongEntityType_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = new string('A', 101);

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "EntityType must be 100 characters or less");
    }

    [Fact]
    public void Validate_WithInvalidPolicyType_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = (DataIsolationPolicyType)999;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "PolicyType must be a valid DataIsolationPolicyType value");
    }

    [Fact]
    public void Validate_WithNegativePriority_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.Priority = -1;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "Priority must be between 0 and 1000");
    }

    [Fact]
    public void Validate_WithPriorityOver1000_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.Priority = 1001;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "Priority must be between 0 and 1000");
    }

    [Fact]
    public void Validate_WithDefaultCreatedAt_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.CreatedAt = default;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "CreatedAt must be set to a valid DateTime");
    }

    [Fact]
    public void Validate_WithFutureCreatedAt_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.CreatedAt = DateTime.UtcNow.AddMinutes(10);

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "CreatedAt cannot be in the future");
    }

    [Fact]
    public void Validate_WithDefaultUpdatedAt_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.UpdatedAt = default;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "UpdatedAt must be set to a valid DateTime");
    }

    [Fact]
    public void Validate_WithFutureUpdatedAt_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.UpdatedAt = DateTime.UtcNow.AddMinutes(10);

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "UpdatedAt cannot be in the future");
    }

    [Fact]
    public void Validate_WithCustomPolicyAndNullFilterRule_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = DataIsolationPolicyType.Custom;
        policy.FilterRule = null;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "FilterRule is required for Custom policy type");
    }

    [Fact]
    public void Validate_WithCustomPolicyAndEmptyFilterRule_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = DataIsolationPolicyType.Custom;
        policy.FilterRule = "";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "FilterRule is required for Custom policy type");
    }


    [Fact]
    public void Validate_WithAllowedCrossTenantAccessContainingInvalidGuid_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.AllowedCrossTenantAccess = "invalid-guid,00000000-0000-0000-0000-000000000000";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "AllowedCrossTenantAccess contains invalid GUID format");
    }

    [Fact]
    public void Validate_WithOverlappingAllowedAndDeniedFields_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.AllowedFields = "Id,Name,Email";
        policy.DeniedFields = "Name,Phone";

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().Contain(e => e.StartsWith("Fields cannot be in both allowed and denied lists"));
        errors.Should().Contain(e => e.Contains("Name"));
    }

    [Fact]
    public void Validate_WithLongDescription_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.Description = new string('A', 1001);

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "Description must be 1000 characters or less");
    }

    [Fact]
    public void Validate_WithLongFilterRule_ReturnsError()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.PolicyType = DataIsolationPolicyType.Custom;
        policy.FilterRule = new string('A', 10001);

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().ContainSingle(e => e == "FilterRule must be 10000 characters or less");
    }

    [Fact]
    public void IsValid_WithNullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        DataIsolationPolicy? policy = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy!.IsValid());
    }

    [Fact]
    public void IsValid_WithValidPolicy_ReturnsTrue()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        var isValid = policy.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidPolicy_ReturnsFalse()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = "";

        // Act
        var isValid = policy.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithNullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        DataIsolationPolicy? policy = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_WithValidPolicy_DoesNotThrow()
    {
        // Arrange
        var policy = CreateValidPolicy();

        // Act
        Action act = () => policy.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithInvalidPolicy_ThrowsArgumentException()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = "";

        // Act
        Action act = () => policy.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*EntityType is required*");
    }

    [Fact]
    public void EnsureValid_WithInvalidPolicy_ContainsAllErrorsInMessage()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.EntityType = "";
        policy.Priority = 2000;

        // Act
        Action act = () => policy.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*EntityType is required*")
            .WithMessage("*Priority must be between 0 and 1000*");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var policy = CreateValidPolicy();
        policy.Id = Guid.Empty;
        policy.TenantId = Guid.Empty;
        policy.EntityType = "";
        policy.PolicyType = (DataIsolationPolicyType)999;

        // Act
        var errors = policy.Validate();

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().Contain(e => e.Contains("Id must be a non-empty GUID"));
        errors.Should().Contain(e => e.Contains("TenantId must be a non-empty GUID"));
        errors.Should().Contain(e => e.Contains("EntityType is required"));
        errors.Should().Contain(e => e.Contains("PolicyType must be a valid DataIsolationPolicyType value"));
    }
}
