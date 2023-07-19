#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 3: Data Isolation Policies
/// Demonstrates creating and enforcing data isolation rules.
/// </summary>
public class DataIsolationPoliciesExample
{
    public static async Task RunAsync(WebApplication app, Guid tenantId)
    {
        using (var scope = app.Services.CreateAsyncScope())
        {
            var isolationService = scope.ServiceProvider
                .GetRequiredService<DataIsolationService>();
            var tenantService = scope.ServiceProvider
                .GetRequiredService<TenantService>();

            Console.WriteLine("=== Data Isolation Policies Example ===\n");

            var tenant = await tenantService.GetTenantAsync(tenantId);
            Console.WriteLine($"Working with tenant: {tenant.Name}\n");

            // Create Strict Policy
            Console.WriteLine("1. Strict Isolation Policy");
            Console.WriteLine("   - No cross-tenant access allowed");
            Console.WriteLine("   - All queries filtered by tenant");
            Console.WriteLine("   - Default for sensitive data\n");

            var strictPolicy = await isolationService.CreatePolicyAsync(
                tenantId,
                entityType: "User",
                policyType: DataIsolationPolicyType.Strict);

            Console.WriteLine($"   ✓ Created strict policy for: {strictPolicy.EntityType}\n");

            // Verify field access
            var canAccessEmail = await isolationService.IsFieldAccessAllowedAsync(
                tenantId, "User", "Email");

            Console.WriteLine($"   Field access check - Email: {canAccessEmail}\n");

            // Create Relaxed Policy
            Console.WriteLine("2. Relaxed Isolation Policy");
            Console.WriteLine("   - Allow specific cross-tenant access");
            Console.WriteLine("   - Useful for shared reference data");
            Console.WriteLine("   - Requires explicit allow-list\n");

            var sharedTenantId = Guid.NewGuid();
            var relaxedPolicy = await isolationService.CreatePolicyAsync(
                tenantId,
                entityType: "ReferenceData",
                policyType: DataIsolationPolicyType.Relaxed,
                allowedCrossTenantAccess: sharedTenantId.ToString());

            Console.WriteLine($"   ✓ Created relaxed policy for: {relaxedPolicy.EntityType}");
            Console.WriteLine($"   Allows access from: {sharedTenantId}\n");

            // Check cross-tenant access
            var canAccessSharedTenant = await isolationService.CanAccessCrossTenantAsync(
                tenantId, sharedTenantId);

            Console.WriteLine($"   Cross-tenant access allowed: {canAccessSharedTenant}\n");

            // Create Custom Policy
            Console.WriteLine("3. Custom Isolation Policy");
            Console.WriteLine("   - Define custom filter rules");
            Console.WriteLine("   - LINQ predicates for complex logic");
            Console.WriteLine("   - Maximum flexibility\n");

            var customPolicy = await isolationService.CreatePolicyAsync(
                tenantId,
                entityType: "Document",
                policyType: DataIsolationPolicyType.Custom);

            Console.WriteLine($"   ✓ Created custom policy for: {customPolicy.EntityType}\n");

            Console.WriteLine("Policy Type Comparison:");
            Console.WriteLine("┌─────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Strict  │ Strict isolation, highest security          │");
            Console.WriteLine("│ Relaxed │ Explicit cross-tenant access allowed        │");
            Console.WriteLine("│ Custom  │ Custom filter rules for complex scenarios    │");
            Console.WriteLine("└─────────────────────────────────────────────────────┘\n");

            Console.WriteLine("=== Example Complete ===");
        }
    }
}
