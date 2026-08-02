using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Tests;

/// <summary>
/// Contains unit tests for the DataIsolationService class, which manages data isolation policies for multi-tenancy.
/// </summary>
public class DataIsolationServiceTests
{
    private readonly TenantDbContext _dbContext;
    private readonly Mock<ILogger<DataIsolationService>> _mockLogger;
    private readonly DataIsolationService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationServiceTests"/> class.
    /// Sets up an in-memory database and mock logger for testing.
    /// </summary>
    public DataIsolationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"DataIsolationServiceTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _mockLogger = new Mock<ILogger<DataIsolationService>>();

        _sut = new DataIsolationService(_dbContext, _mockLogger.Object);
    }

    /// <summary>
    /// Tests that creating a policy with valid parameters returns the created policy.
    /// </summary>
    [Fact]
    public async Task CreatePolicyAsync_WithValidPolicy_ReturnsPolicy()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.CreatePolicyAsync(tenantId, "Order", DataIsolationPolicyType.Strict);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
        result.EntityType.Should().Be("Order");
        result.PolicyType.Should().Be(DataIsolationPolicyType.Strict);
    }

    /// <summary>
    /// Tests that retrieving an existing policy returns the policy.
    /// </summary>
    [Fact]
    public async Task GetPolicyAsync_WithExistingPolicy_ReturnsPolicy()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Customer",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetPolicyAsync(tenantId, "Customer");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.EntityType.Should().Be("Customer");
    }

    /// <summary>
    /// Tests that retrieving a non-existing policy returns null.
    /// </summary>
    [Fact]
    public async Task GetPolicyAsync_WithNonExistingPolicy_ReturnsNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.GetPolicyAsync(tenantId, "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that field access is allowed when no policy exists for the entity type.
    /// </summary>
    [Fact]
    public async Task IsFieldAccessAllowedAsync_WithNoPolicy_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.IsFieldAccessAllowedAsync(tenantId, "Order", "Amount");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that field access is denied when the field is explicitly denied in the policy.
    /// </summary>
    [Fact]
    public async Task IsFieldAccessAllowedAsync_WithDeniedField_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            DeniedFields = "Amount,Total",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.IsFieldAccessAllowedAsync(tenantId, "Order", "Amount");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that field access is allowed when the field is explicitly allowed in the policy.
    /// </summary>
    [Fact]
    public async Task IsFieldAccessAllowedAsync_WithAllowedField_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            AllowedFields = "Id,CustomerId",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.IsFieldAccessAllowedAsync(tenantId, "Order", "CustomerId");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that verifying field access does not throw when access is allowed.
    /// </summary>
    [Fact]
    public async Task VerifyFieldAccessAsync_WithAllowedAccess_DoesNotThrow()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            AllowedFields = "Id,CustomerId",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.VerifyFieldAccessAsync(tenantId, "Order", "CustomerId");

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that verifying field access throws DataIsolationViolationException when access is denied.
    /// </summary>
    [Fact]
    public async Task VerifyFieldAccessAsync_WithDeniedAccess_ThrowsDataIsolationViolationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            DeniedFields = "Amount,Total",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.VerifyFieldAccessAsync(tenantId, "Order", "Amount");

        // Assert
        await act.Should().ThrowAsync<DataIsolationViolationException>()
            .Where(e => e.TenantId == tenantId)
            .Where(e => e.EntityType == "Order");
    }

    /// <summary>
    /// Tests that cross-tenant access is denied when the policy is strict.
    /// </summary>
    [Fact]
    public async Task CanAccessCrossTenantAsync_WithStrictPolicy_ReturnsFalse()
    {
        // Arrange
        var currentTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.CanAccessCrossTenantAsync(currentTenantId, targetTenantId, "Order");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that cross-tenant access is denied when the policy is relaxed but no tenants are allowed.
    /// </summary>
    [Fact]
    public async Task CanAccessCrossTenantAsync_WithRelaxedPolicyAndNoAllowedTenants_ReturnsFalse()
    {
        // Arrange
        var currentTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            PolicyType = DataIsolationPolicyType.Relaxed,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.CanAccessCrossTenantAsync(currentTenantId, targetTenantId, "Order");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that cross-tenant access is allowed when the policy is relaxed and the target tenant is explicitly allowed.
    /// </summary>
    [Fact]
    public async Task CanAccessCrossTenantAsync_WithRelaxedPolicyAndAllowedTenant_ReturnsTrue()
    {
        // Arrange
        var currentTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            PolicyType = DataIsolationPolicyType.Relaxed,
            EntityType = "Order",
            AllowedCrossTenantAccess = $"{targetTenantId}",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.CanAccessCrossTenantAsync(currentTenantId, targetTenantId, "Order");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that updating a policy with valid changes returns the updated policy.
    /// </summary>
    [Fact]
    public async Task UpdatePolicyAsync_WithValidUpdate_ReturnsUpdatedPolicy()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.UpdatePolicyAsync(policy.Id, p => p.Description = "Updated description");

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated description");
        var savedPolicy = await _dbContext.DataIsolationPolicies.FindAsync(policy.Id);
        savedPolicy!.UpdatedAt.Should().BeAfter(savedPolicy.CreatedAt);
    }

    /// <summary>
    /// Tests that deleting an existing policy returns true and removes the policy from the database.
    /// </summary>
    [Fact]
    public async Task DeletePolicyAsync_WithExistingPolicy_ReturnsTrueAndRemovesPolicy()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeletePolicyAsync(policy.Id);

        // Assert
        result.Should().BeTrue();
        var deletedPolicy = await _dbContext.DataIsolationPolicies.FindAsync(policy.Id);
        deletedPolicy.Should().BeNull();
    }

    /// <summary>
    /// Tests that getting active policies returns only the active policies for a tenant.
    /// </summary>
    [Fact]
    public async Task GetActivePoliciesAsync_WithMultiplePolicies_ReturnsOnlyActivePolicies()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var activePolicy1 = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        var activePolicy2 = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Relaxed,
            EntityType = "Customer",
            IsActive = true
        };
        var inactivePolicy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Product",
            IsActive = false
        };
        await _dbContext.DataIsolationPolicies.AddRangeAsync(activePolicy1, activePolicy2, inactivePolicy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetActivePoliciesAsync(tenantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == activePolicy1.Id);
        result.Should().Contain(p => p.Id == activePolicy2.Id);
        result.Should().NotContain(p => p.Id == inactivePolicy.Id);
    }

    /// <summary>
    /// Tests that setting a policy's active status updates the status correctly.
    /// </summary>
    [Fact]
    public async Task SetPolicyActiveAsync_WithExistingPolicy_UpdatesActiveStatus()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.SetPolicyActiveAsync(policy.Id, false);

        // Assert
        result.Should().BeTrue();
        var savedPolicy = await _dbContext.DataIsolationPolicies.FindAsync(policy.Id);
        savedPolicy!.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Tests that setting a policy's priority updates the priority correctly.
    /// </summary>
    [Fact]
    public async Task SetPolicyPriorityAsync_WithValidPriority_UpdatesPriority()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            Priority = 100,
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.SetPolicyPriorityAsync(policy.Id, 50);

        // Assert
        result.Should().BeTrue();
        var savedPolicy = await _dbContext.DataIsolationPolicies.FindAsync(policy.Id);
        savedPolicy!.Priority.Should().Be(50);
    }

    /// <summary>
    /// Tests that checking policy violations returns an empty list when no policy exists.
    /// </summary>
    [Fact]
    public async Task CheckPolicyViolationsAsync_WithNoPolicy_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var entityData = new { Id = 1, Name = "Test" };

        // Act
        var result = await _sut.CheckPolicyViolationsAsync(tenantId, "Order", entityData);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that checking policy violations returns violations for denied fields.
    /// </summary>
    [Fact]
    public async Task CheckPolicyViolationsAsync_WithDeniedField_ReturnsViolation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            DeniedFields = "Amount,Total",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        var entityData = new { Id = 1, Amount = 100, Total = 200 };

        // Act
        var result = await _sut.CheckPolicyViolationsAsync(tenantId, "Order", entityData);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(item => item.Contains("Amount"));
        result.Should().Contain(item => item.Contains("Total"));
    }

    /// <summary>
    /// Tests that exporting an existing policy returns a JSON representation of the policy.
    /// </summary>
    [Fact]
    public async Task ExportPolicyAsync_WithExistingPolicy_ReturnsJson()
    {
        // Arrange
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PolicyType = DataIsolationPolicyType.Strict,
            EntityType = "Order",
            IsActive = true
        };
        await _dbContext.DataIsolationPolicies.AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.ExportPolicyAsync(policy.Id);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Order");
    }
}