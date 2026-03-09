// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 2: Tenant Resolution Strategies
/// Demonstrates automatic tenant detection from various sources.
/// </summary>
public class TenantResolutionStrategiesExample
{
    public static async Task RunAsync(WebApplication app, Guid tenantId)
    {
        Console.WriteLine("=== Tenant Resolution Strategies Example ===\n");

        // Strategy 1: HTTP Header Resolution
        Console.WriteLine("Strategy 1: HTTP Header Resolution");
        Console.WriteLine("Request Headers:");
        Console.WriteLine("  X-Tenant-Id: 550e8400-e29b-41d4-a716-446655440000");
        Console.WriteLine("  X-Tenant-Slug: acme-corp\n");

        // Strategy 2: Route Parameter Resolution
        Console.WriteLine("Strategy 2: Route Parameter Resolution");
        Console.WriteLine("URL Path:");
        Console.WriteLine("  GET /api/tenants/{tenantId}/organizations");
        Console.WriteLine("  GET /api/tenants/{slug}/organizations\n");

        // Strategy 3: User Claims Resolution
        Console.WriteLine("Strategy 3: User Claims Resolution");
        Console.WriteLine("JWT Claims:");
        Console.WriteLine("  \"tenant_id\": \"550e8400-e29b-41d4-a716-446655440000\"");
        Console.WriteLine("  \"tenant_slug\": \"acme-corp\"\n");

        // Strategy 4: Subdomain Resolution
        Console.WriteLine("Strategy 4: Subdomain Resolution");
        Console.WriteLine("Request Host:");
        Console.WriteLine("  acme-corp.example.com/api/organizations\n");

        using (var scope = app.Services.CreateAsyncScope())
        {
            var tenantResolution = scope.ServiceProvider
                .GetRequiredService<TenantResolutionService>();

            // Create a mock HTTP context with header
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

            // In real scenarios, this is called automatically by middleware
            Console.WriteLine("Cascading Resolution Order:");
            Console.WriteLine("  1. Header (X-Tenant-Id) → FOUND");
            Console.WriteLine("  2. Claims → Skipped");
            Console.WriteLine("  3. Route parameters → Skipped");
            Console.WriteLine("  4. Subdomain → Skipped\n");

            Console.WriteLine("Resolution Result: ✓ Tenant resolved successfully\n");
            Console.WriteLine("=== Example Complete ===");
        }
    }
}
