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
/// Unit tests for the <see cref="DataIsolationPolicy"/> class.
/// Tests all public members including properties, field parsing methods, and validation.
/// </summary>
public class DataIsolationPolicyTests
{
    private static DataIsolationPolicy CreateTestPolicy(
        Guid? tenantId = null,
        DataIsolationPolicyType? policyType = null,
        string? entityType = null,
        string? description = null,
        string? filterRule = null,
        string? allowedFields = null,
        string? deniedFields = null,
        string? allowedCrossTenantAccess = null,
        bool? isActive = null)
    {
        return new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            PolicyType = policyType ?? DataIsolationPolicyType.Strict,
            EntityType = entityType ?? "Order",
            Description = description,
            FilterRule = filterRule,
            AllowedFields = allowedFields,
            DeniedFields = deniedFields,
            AllowedCrossTenantAccess = allowedCrossTenantAccess,
            IsActive = isActive ?? true
        };
    }

    [Fact]
    public void Constructor_DefaultValues_ShouldInitializeCorrectly()
    {
        // Act
        var policy = CreateTestPolicy();

        // Assert
        policy.Id.Should().NotBe(Guid.Empty);
        policy.TenantId.Should().NotBe(Guid.Empty);
        policy.PolicyType.Should().Be(DataIsolationPolicyType.Strict);
        policy.EntityType.Should().Be("Order");
        policy.Description.Should().BeNull();
        policy.FilterRule.Should().BeNull();
        policy.AllowedFields.Should().BeNull();
        policy.DeniedFields.Should().BeNull();
        policy.AllowedCrossTenantAccess.Should().BeNull();
        policy.IsActive.Should().BeTrue();
        policy.Priority.Should().Be(100);
        policy.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        policy.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var policyType = DataIsolationPolicyType.Relaxed;
        var entityType = "Customer";
        var description = "Customer data isolation policy";
        var filterRule = "WHERE TenantId = @tenantId";
        var allowedFields = "Id,Name,Email";
        var deniedFields = "Password,Ssn";
        var allowedCrossTenantAccess = "tenant-123,tenant-456";
        var isActive = false;
        var priority = 50;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var policy = new DataIsolationPolicy
        {
            Id = id,
            TenantId = tenantId,
            PolicyType = policyType,
            EntityType = entityType,
            Description = description,
            FilterRule = filterRule,
            AllowedFields = allowedFields,
            DeniedFields = deniedFields,
            AllowedCrossTenantAccess = allowedCrossTenantAccess,
            IsActive = isActive,
            Priority = priority,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        policy.Id.Should().Be(id);
        policy.TenantId.Should().Be(tenantId);
        policy.PolicyType.Should().Be(policyType);
        policy.EntityType.Should().Be(entityType);
        policy.Description.Should().Be(description);
        policy.FilterRule.Should().Be(filterRule);
        policy.AllowedFields.Should().Be(allowedFields);
        policy.DeniedFields.Should().Be(deniedFields);
        policy.AllowedCrossTenantAccess.Should().Be(allowedCrossTenantAccess);
        policy.IsActive.Should().Be(isActive);
        policy.Priority.Should().Be(priority);
        policy.CreatedAt.Should().Be(createdAt);
        policy.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Properties_GetAndSet_RoundTripCorrectly()
    {
        // Arrange
        var policy = CreateTestPolicy();
        var newId = Guid.NewGuid();
        var newTenantId = Guid.NewGuid();
        var newEntityType = "Product";
        var newDescription = "Product isolation";
        var newFilterRule = "WHERE TenantId = @tenant";
        var newAllowedFields = "Id,Name";
        var newDeniedFields = "Price";
        var newAllowedCrossTenantAccess = "tenant-789";

        // Act
        policy.Id = newId;
        policy.TenantId = newTenantId;
        policy.PolicyType = DataIsolationPolicyType.Custom;
        policy.EntityType = newEntityType;
        policy.Description = newDescription;
        policy.FilterRule = newFilterRule;
        policy.AllowedFields = newAllowedFields;
        policy.DeniedFields = newDeniedFields;
        policy.AllowedCrossTenantAccess = newAllowedCrossTenantAccess;
        policy.IsActive = false;
        policy.Priority = 10;

        // Assert
        policy.Id.Should().Be(newId);
        policy.TenantId.Should().Be(newTenantId);
        policy.PolicyType.Should().Be(DataIsolationPolicyType.Custom);
        policy.EntityType.Should().Be(newEntityType);
        policy.Description.Should().Be(newDescription);
        policy.FilterRule.Should().Be(newFilterRule);
        policy.AllowedFields.Should().Be(newAllowedFields);
        policy.DeniedFields.Should().Be(newDeniedFields);
        policy.AllowedCrossTenantAccess.Should().Be(newAllowedCrossTenantAccess);
        policy.IsActive.Should().BeFalse();
        policy.Priority.Should().Be(10);
    }

    [Fact]
    public void GetAllowedFields_WithNullAllowedFields_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: null);

        // Act
        var result = policy.GetAllowedFields();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedFields_WithEmptyAllowedFields_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "");

        // Act
        var result = policy.GetAllowedFields();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedFields_WithWhitespaceAllowedFields_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "   ");

        // Act
        var result = policy.GetAllowedFields();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedFields_WithValidFields_ReturnsParsedList()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "Id, Name, Email, Phone");

        // Act
        var result = policy.GetAllowedFields();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain("Id");
        result.Should().Contain("Name");
        result.Should().Contain("Email");
        result.Should().Contain("Phone");
    }

    [Fact]
    public void GetAllowedFields_WithExtraWhitespace_TrimsFields()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "  Id  ,  Name  , Email  ");

        // Act
        var result = policy.GetAllowedFields();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Id");
        result.Should().Contain("Name");
        result.Should().Contain("Email");
    }

    [Fact]
    public void GetDeniedFields_WithNullDeniedFields_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateTestPolicy(deniedFields: null);

        // Act
        var result = policy.GetDeniedFields();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDeniedFields_WithEmptyDeniedFields_ReturnsEmptyList()
    {
        // Arrange
        var policy = CreateTestPolicy(deniedFields: "");

        // Act
        var result = policy.GetDeniedFields();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDeniedFields_WithValidFields_ReturnsParsedList()
    {
        // Arrange
        var policy = CreateTestPolicy(deniedFields: "Password,Ssn,CreditCard");

        // Act
        var result = policy.GetDeniedFields();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Password");
        result.Should().Contain("Ssn");
        result.Should().Contain("CreditCard");
    }

    [Fact]
    public void IsFieldAccessAllowed_WithNoRestrictions_ReturnsTrue()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: null, deniedFields: null);

        // Act
        var result = policy.IsFieldAccessAllowed("AnyField");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithFieldInDeniedList_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(deniedFields: "Password,SecretKey");

        // Act
        var result = policy.IsFieldAccessAllowed("Password");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithCaseInsensitiveDeniedField_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(deniedFields: "password,secretkey");

        // Act
        var result = policy.IsFieldAccessAllowed("PASSWORD");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithAllowedFieldsList_ReturnsTrueForAllowedField()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "Id,Name,Email");

        // Act
        var result = policy.IsFieldAccessAllowed("Name");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithAllowedFieldsList_ReturnsFalseForNotAllowedField()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "Id,Name,Email");

        // Act
        var result = policy.IsFieldAccessAllowed("Password");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithBothAllowedAndDeniedLists_ReturnsFalseForDeniedField()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "Id,Name,Email", deniedFields: "Email,Phone");

        // Act
        var result = policy.IsFieldAccessAllowed("Email");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFieldAccessAllowed_WithEmptyAllowedList_ReturnsTrueForAnyField()
    {
        // Arrange
        var policy = CreateTestPolicy(allowedFields: "", deniedFields: null);

        // Act
        var result = policy.IsFieldAccessAllowed("AnyField");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithStrictPolicy_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(policyType: DataIsolationPolicyType.Strict);

        // Act
        var result = policy.IsCrossTenantAccessAllowed(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithRelaxedPolicyAndNoAllowedTenants_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Relaxed,
            allowedCrossTenantAccess: null
        );

        // Act
        var result = policy.IsCrossTenantAccessAllowed(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithRelaxedPolicyAndAllowedTenant_ReturnsTrue()
    {
        // Arrange
        var allowedTenantId = Guid.NewGuid();
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Relaxed,
            allowedCrossTenantAccess: $"{allowedTenantId}"
        );

        // Act
        var result = policy.IsCrossTenantAccessAllowed(allowedTenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithRelaxedPolicyAndNotAllowedTenant_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Relaxed,
            allowedCrossTenantAccess: "00000000-0000-0000-0000-000000000000,11111111-1111-1111-1111-111111111111"
        );

        // Act
        var result = policy.IsCrossTenantAccessAllowed(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithCustomPolicyAndNoAllowedTenants_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Custom,
            allowedCrossTenantAccess: null
        );

        // Act
        var result = policy.IsCrossTenantAccessAllowed(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCrossTenantAccessAllowed_WithCustomPolicyAndAllowedTenant_ReturnsTrue()
    {
        // Arrange
        var allowedTenantId = Guid.NewGuid();
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Custom,
            allowedCrossTenantAccess: $"{allowedTenantId}"
        );

        // Act
        var result = policy.IsCrossTenantAccessAllowed(allowedTenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidPolicy_WithValidStrictPolicy_ReturnsTrue()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Strict,
            entityType: "Order"
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void IsValidPolicy_WithValidCustomPolicy_ReturnsTrue()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Custom,
            entityType: "Customer",
            filterRule: "WHERE TenantId = @tenantId"
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void IsValidPolicy_WithEmptyEntityType_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(entityType: "");

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Entity type is required");
    }

    [Fact]
    public void IsValidPolicy_WithWhitespaceEntityType_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(entityType: "   ");

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Entity type is required");
    }

    [Fact]
    public void IsValidPolicy_WithNullEntityType_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy();
        policy.EntityType = null!;

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Entity type is required");
    }

    [Fact]
    public void IsValidPolicy_WithCustomPolicyAndEmptyFilterRule_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Custom,
            entityType: "Customer",
            filterRule: ""
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Filter rule is required for custom policies");
    }

    [Fact]
    public void IsValidPolicy_WithCustomPolicyAndNullFilterRule_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            policyType: DataIsolationPolicyType.Custom,
            entityType: "Customer",
            filterRule: null
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Be("Filter rule is required for custom policies");
    }

    [Fact]
    public void IsValidPolicy_WithOverlappingAllowedAndDeniedFields_ReturnsFalse()
    {
        // Arrange
        var policy = CreateTestPolicy(
            allowedFields: "Id,Name,Email",
            deniedFields: "Name,Phone"
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("Fields cannot be in both allowed and denied lists");
        errorMessage.Should().Contain("Name");
    }

    [Fact]
    public void IsValidPolicy_WithNonOverlappingAllowedAndDeniedFields_ReturnsTrue()
    {
        // Arrange
        var policy = CreateTestPolicy(
            allowedFields: "Id,Name",
            deniedFields: "Password,Ssn"
        );

        // Act
        var isValid = policy.IsValidPolicy(out var errorMessage);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void TenantNavigationProperty_ShouldBeNullByDefault()
    {
        // Arrange
        var policy = CreateTestPolicy();

        // Assert
        policy.Tenant.Should().BeNull();
    }

    [Fact]
    public void TenantNavigationProperty_ShouldBeSettable()
    {
        // Arrange
        var policy = CreateTestPolicy();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant" };

        // Act
        policy.Tenant = tenant;

        // Assert
        policy.Tenant.Should().BeSameAs(tenant);
    }

    [Fact]
    public void CreatedAt_ShouldBeSetToUtcNowByDefault()
    {
        // Arrange & Act
        var policy = new DataIsolationPolicy();

        // Assert
        policy.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatedAt_ShouldBeSetToUtcNowByDefault()
    {
        // Arrange & Act
        var policy = new DataIsolationPolicy();

        // Assert
        policy.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Priority_ShouldHaveDefaultValue()
    {
        // Arrange & Act
        var policy = new DataIsolationPolicy();

        // Assert
        policy.Priority.Should().Be(100);
    }

    [Fact]
    public void Priority_ShouldBeSettable()
    {
        // Arrange
        var policy = CreateTestPolicy();

        // Act
        policy.Priority = 50;

        // Assert
        policy.Priority.Should().Be(50);
    }
}