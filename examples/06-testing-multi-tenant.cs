// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 6: Testing Multi-Tenant Applications
/// Demonstrates unit and integration testing patterns.
/// </summary>
public class TestingMultiTenantExample
{
    /// <summary>
    /// Setup in-memory database for testing
    /// </summary>
    public static async Task<ServiceProvider> SetupInMemoryTestDatabaseAsync()
    {
        var services = new ServiceCollection();

        // Add TenantIsolation with in-memory database
        services.AddTenantIsolationInMemory("TestDb", options =>
        {
            options.AutoMigrate = true;
            options.EnableAuditLogging = false;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Initialize database
        using (var scope = serviceProvider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        return serviceProvider;
    }

    /// <summary>
    /// Example unit test for tenant creation
    /// </summary>
    public static async Task TestTenantCreationAsync()
    {
        Console.WriteLine("=== Testing Multi-Tenant Applications ===\n");

        var serviceProvider = await SetupInMemoryTestDatabaseAsync();

        try
        {
            using (var scope = serviceProvider.CreateAsyncScope())
            {
                var tenantService = scope.ServiceProvider
                    .GetRequiredService<TenantService>();

                // Arrange
                var name = "Test Tenant";
                var slug = "test-tenant";
                var email = "admin@test.com";

                // Act
                var tenant = await tenantService.CreateTenantAsync(name, slug, email);

                // Assert
                Console.WriteLine("Test 1: Create Tenant");
                Console.WriteLine($"  ✓ Tenant created: {tenant.Name}");
                Console.WriteLine($"  ✓ Tenant ID: {tenant.Id}");
                Console.WriteLine($"  ✓ Tenant slug: {tenant.Slug}");
                Console.WriteLine($"  ✓ Tenant status: {tenant.Status}\n");

                // Test tenant retrieval
                var retrieved = await tenantService.GetTenantAsync(tenant.Id);
                Console.WriteLine("Test 2: Retrieve Tenant");
                Console.WriteLine($"  ✓ Retrieved tenant ID matches: {retrieved.Id == tenant.Id}");
                Console.WriteLine($"  ✓ Retrieved tenant name matches: {retrieved.Name == tenant.Name}\n");

                // Test tenant activation
                await tenantService.ActivateTenantAsync(tenant.Id);
                var activated = await tenantService.GetTenantAsync(tenant.Id);
                Console.WriteLine("Test 3: Activate Tenant");
                Console.WriteLine($"  ✓ Tenant is active: {activated.Status == TenantStatus.Active}\n");

                // Test subscription validity
                var subscriptionValid = await tenantService.IsSubscriptionValidAsync(tenant.Id);
                Console.WriteLine("Test 4: Check Subscription");
                Console.WriteLine($"  ✓ Subscription is valid: {subscriptionValid}\n");
            }
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Example integration test with multiple tenants
    /// </summary>
    public static async Task TestMultipleTenantIsolationAsync()
    {
        var serviceProvider = await SetupInMemoryTestDatabaseAsync();

        try
        {
            using (var scope = serviceProvider.CreateAsyncScope())
            {
                var tenantService = scope.ServiceProvider
                    .GetRequiredService<TenantService>();
                var configService = scope.ServiceProvider
                    .GetRequiredService<ConfigurationService>();

                // Create two tenants
                var tenant1 = await tenantService.CreateTenantAsync(
                    "Company A", "company-a", "admin@a.com");
                var tenant2 = await tenantService.CreateTenantAsync(
                    "Company B", "company-b", "admin@b.com");

                Console.WriteLine("Test 5: Tenant Isolation");
                Console.WriteLine($"  ✓ Created tenant 1: {tenant1.Name}");
                Console.WriteLine($"  ✓ Created tenant 2: {tenant2.Name}\n");

                // Set different configurations per tenant
                await configService.SetConfigurationAsync(
                    tenant1.Id, "company:color", "blue");
                await configService.SetConfigurationAsync(
                    tenant2.Id, "company:color", "red");

                // Verify isolation
                var color1 = await configService.GetConfigurationAsync<string>(
                    tenant1.Id, "company:color");
                var color2 = await configService.GetConfigurationAsync<string>(
                    tenant2.Id, "company:color");

                Console.WriteLine("Test 6: Configuration Isolation");
                Console.WriteLine($"  ✓ Tenant 1 color: {color1}");
                Console.WriteLine($"  ✓ Tenant 2 color: {color2}");
                Console.WriteLine($"  ✓ Configurations properly isolated: {color1 != color2}\n");
            }
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    public static async Task RunAsync()
    {
        await TestTenantCreationAsync();
        await TestMultipleTenantIsolationAsync();

        Console.WriteLine("Testing Best Practices:");
        Console.WriteLine("┌───────────────────────────────────────────────────────┐");
        Console.WriteLine("│ Use in-memory database for unit tests                 │");
        Console.WriteLine("│ Create separate test tenants for isolation tests      │");
        Console.WriteLine("│ Verify data doesn't leak between tenants              │");
        Console.WriteLine("│ Test both happy path and error cases                  │");
        Console.WriteLine("│ Mock external dependencies (email, payment, etc.)      │");
        Console.WriteLine("│ Test tenant resolution with different strategies      │");
        Console.WriteLine("│ Load test with realistic tenant and data volumes      │");
        Console.WriteLine("└───────────────────────────────────────────────────────┘\n");

        Console.WriteLine("=== Example Complete ===");
    }
}
