#nullable enable

using FluentAssertions;
using Moq;
using TenantIsolation.Constants;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Tests;

/// <summary>
/// Extension methods for <see cref="TenantServiceTests"/> that provide reusable test utilities
/// for testing <see cref="TenantService"/> functionality.
/// </summary>
public static class TenantServiceTestsExtensions
{
    /// <summary>
    /// Creates a mock tenant with default values for testing.
    /// </summary>
    /// <param name="id">Optional tenant ID. If null, a new GUID is generated.</param>
    /// <param name="name">Tenant name. Defaults to "Test Tenant".</param>
    /// <param name="slug">Tenant slug. Defaults to "test-tenant".</param>
    /// <param name="status">Tenant status. Defaults to <see cref="TenantStatus.Active"/>.</param>
    /// <returns>A configured tenant object ready for test assertions.</returns>
    public static Tenant CreateMockTenant(
        this TenantServiceTests _,
        Guid? id = null,
        string name = "Test Tenant",
        string slug = "test-tenant",
        TenantStatus status = TenantStatus.Active)
    {
        return new Tenant
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Status = status,
            AdminEmail = "admin@example.com",
            IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Sets up the mock repository to return a specific tenant when queried by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID to match.</param>
    /// <param name="tenant">The tenant to return.</param>
    /// <returns>The same <see cref="TenantServiceTests"/> instance for method chaining.</returns>
    public static TenantServiceTests SetupGetTenantById(
        this TenantServiceTests tests,
        Guid tenantId,
        Tenant tenant)
    {
        tests._mockDynamicTenantStore
            .Setup(s => s.GetTenantByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        return tests;
    }

    /// <summary>
    /// Sets up the mock repository to return a specific tenant when queried by slug.
    /// </summary>
    /// <param name="slug">The tenant slug to match (case-insensitive).</param>
    /// <param name="tenant">The tenant to return.</param>
    /// <returns>The same <see cref="TenantServiceTests"/> instance for method chaining.</returns>
    public static TenantServiceTests SetupGetTenantBySlug(
        this TenantServiceTests tests,
        string slug,
        Tenant tenant)
    {
        var activeTenants = new List<Tenant> { tenant };

        tests._mockDynamicTenantStore
            .Setup(s => s.GetAllActiveTenantsAsync())
            .ReturnsAsync(activeTenants);

        return tests;
    }

    /// <summary>
    /// Verifies that the tenant repository's ActivateTenantAsync was called exactly once with the specified tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID that should have been activated.</param>
    /// <param name="times">Optional verification times. Defaults to once.</param>
    /// <returns>The same <see cref="TenantServiceTests"/> instance for method chaining.</returns>
    public static TenantServiceTests VerifyActivateTenantCalled(
        this TenantServiceTests tests,
        Guid tenantId,
        Times? times = null)
    {
        times ??= Times.Once();

        tests._mockTenantRepository.Verify(
            r => r.ActivateTenantAsync(tenantId),
            times.Value);

        return tests;
    }

    /// <summary>
    /// Creates a list of mock tenants for testing collection operations.
    /// </summary>
    /// <param name="count">Number of tenants to create. Defaults to 3.</param>
    /// <param name="status">Status for all tenants. Defaults to <see cref="TenantStatus.Active"/>.</param>
    /// <returns>An <see cref="IReadOnlyList{Tenant}"/> of configured tenants.</returns>
    public static IReadOnlyList<Tenant> CreateMockTenants(
        this TenantServiceTests _,
        int count = 3,
        TenantStatus status = TenantStatus.Active)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        var tenants = new List<Tenant>(count);
        for (var i = 0; i < count; i++)
        {
            tenants.Add(new Tenant
            {
                Id = Guid.NewGuid(),
                Name = $"Tenant {i + 1}",
                Slug = $"tenant-{i + 1}",
                Status = status,
                AdminEmail = $"admin{i + 1}@example.com",
                IsolationStrategy = TenantIsolationStrategy.DatabasePerTenant,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        return tenants.AsReadOnly();
    }
}
