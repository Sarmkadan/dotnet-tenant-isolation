#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using TenantIsolation.Models;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Extension methods for <see cref="TenantResolutionBenchmarks"/> providing additional benchmark scenarios
/// that complement the core tenant resolution operations.
/// </summary>
public static class TenantResolutionBenchmarksExtensions
{
    /// <summary>
    /// Benchmark multi-tenant scenario: Resolve multiple tenants sequentially.
    /// Tests the overhead of tenant resolution in a multi-tenant environment.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if tenant resolution fails.</exception>
    [Benchmark]
    public static async ValueTask ResolveMultipleTenants_Sequential()
    {
        var benchmarks = new TenantResolutionBenchmarks();
        benchmarks.Setup();

        try
        {
            // Resolve tenants for multiple requests
            var tenant1 = await benchmarks.ResolveTenant_FromHeader();
            var tenant2 = await benchmarks.ResolveTenant_FromRoute();
            var tenant3 = await benchmarks.ResolveTenant_FromClaims();

            // Verify we got valid tenants
            if (tenant1 is null || tenant2 is null || tenant3 is null)
            {
                throw new InvalidOperationException("Failed to resolve tenants");
            }
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }

    /// <summary>
    /// Benchmark tenant resolution with invalid tenant ID.
    /// Tests error handling and validation performance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if tenant resolution does not return null for invalid tenant ID.</exception>
    [Benchmark]
    public static async ValueTask ResolveTenant_InvalidId()
    {
        var benchmarks = new TenantResolutionBenchmarks();
        benchmarks.Setup();

        try
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Tenant-Id"] = Guid.Empty.ToString();
            var accessor = new HttpContextAccessor { HttpContext = httpContext };

            // Use reflection to set the HttpContextAccessor in the benchmarks instance
            var accessorField = typeof(TenantResolutionBenchmarks).GetField(
                "_httpContextAccessor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            ArgumentNullException.ThrowIfNull(accessorField);
            accessorField.SetValue(benchmarks, accessor);

            var result = await benchmarks.ResolveTenant_FromHeader();
            if (result is not null)
            {
                throw new InvalidOperationException("Should return null for invalid tenant ID");
            }
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }

    /// <summary>
    /// Benchmark tenant resolution with tenant switching scenario.
    /// Tests the overhead of changing tenants in a single request flow.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if both tenants resolve to the same ID.</exception>
    [Benchmark]
    public static async ValueTask ResolveTenant_SwitchContext()
    {
        var benchmarks = new TenantResolutionBenchmarks();
        benchmarks.Setup();

        try
        {
            var tenant1 = await benchmarks.ResolveTenant_FromHeader();
            var tenant2 = await benchmarks.ResolveTenant_FromRoute();

            // Verify different tenants
            if (tenant1?.Id == tenant2?.Id)
            {
                throw new InvalidOperationException("Should resolve different tenants");
            }
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }

    /// <summary>
    /// Benchmark current tenant retrieval after resolution.
    /// Tests the fast path after tenant has been resolved.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if tenant resolution or current tenant retrieval fails.</exception>
    [Benchmark]
    public static async ValueTask GetCurrentTenant_Performance()
    {
        var benchmarks = new TenantResolutionBenchmarks();
        benchmarks.Setup();

        try
        {
            // First resolve a tenant
            var tenant = await benchmarks.ResolveTenant_FromHeader();

            if (tenant is null)
            {
                throw new InvalidOperationException("Failed to resolve tenant");
            }

            // Now get current tenant (should be cached/fast path)
            var currentTenant = benchmarks.GetCurrentTenant();

            if (currentTenant is null || currentTenant.Id != tenant.Id)
            {
                throw new InvalidOperationException("Current tenant retrieval failed");
            }
        }
        finally
        {
            benchmarks.Cleanup();
        }
    }
}
