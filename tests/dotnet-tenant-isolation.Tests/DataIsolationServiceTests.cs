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

public class DataIsolationServiceTests
{
    private readonly TenantDbContext _dbContext;
    private readonly Mock<ILogger<DataIsolationService>> _mockLogger;
    private readonly DataIsolationService _sut;

    public DataIsolationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"DataIsolationServiceTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _mockLogger = new Mock<ILogger<DataIsolationService>>();

        _sut = new DataIsolationService(_dbContext, _mockLogger.Object);
    }

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