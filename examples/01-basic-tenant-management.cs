#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 1: Basic Tenant Management
/// Demonstrates creating, retrieving, and managing tenants.
/// </summary>
public class BasicTenantManagementExample
{
    public static async Task RunAsync(WebApplication app)
    {
        using (var scope = app.Services.CreateAsyncScope())
        {
            var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();

            Console.WriteLine("=== Basic Tenant Management Example ===\n");

            // Create tenants
            var tenant1 = await tenantService.CreateTenantAsync(
                "Acme Corporation",
                "acme-corp",
                "admin@acme.com");

            Console.WriteLine($"✓ Created tenant: {tenant1.Name} ({tenant1.Slug})");
            Console.WriteLine($"  ID: {tenant1.Id}");
            Console.WriteLine($"  Status: {tenant1.Status}\n");

            var tenant2 = await tenantService.CreateTenantAsync(
                "TechStart Inc",
                "techstart-inc",
                "admin@techstart.com");

            Console.WriteLine($"✓ Created tenant: {tenant2.Name}\n");

            // Retrieve tenant by ID
            var retrieved = await tenantService.GetTenantAsync(tenant1.Id);
            Console.WriteLine($"✓ Retrieved tenant by ID: {retrieved.Name}\n");

            // Retrieve tenant by slug
            var bySlug = await tenantService.GetTenantBySlugAsync("acme-corp");
            Console.WriteLine($"✓ Retrieved tenant by slug: {bySlug.Name}\n");

            // Get tenant statistics
            var stats = await tenantService.GetTenantStatisticsAsync(tenant1.Id);
            Console.WriteLine($"✓ Tenant Statistics:");
            Console.WriteLine($"  Users: {stats.UserCount}");
            Console.WriteLine($"  Organizations: {stats.OrganizationCount}");
            Console.WriteLine($"  Storage: {stats.StorageUsedMb}MB\n");

            // Activate and suspend
            await tenantService.ActivateTenantAsync(tenant1.Id);
            Console.WriteLine($"✓ Activated tenant\n");

            // Check subscription validity
            var isValid = await tenantService.IsSubscriptionValidAsync(tenant1.Id);
            Console.WriteLine($"✓ Subscription valid: {isValid}\n");

            Console.WriteLine("=== Example Complete ===");
        }
    }
}
