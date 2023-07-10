#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 4: Configuration Management
/// Demonstrates per-tenant configuration storage and retrieval.
/// </summary>
public class ConfigurationManagementExample
{
    public static async Task RunAsync(WebApplication app, Guid tenantId)
    {
        using (var scope = app.Services.CreateAsyncScope())
        {
            var configService = scope.ServiceProvider
                .GetRequiredService<ConfigurationService>();

            Console.WriteLine("=== Configuration Management Example ===\n");

            // Set various configuration values
            Console.WriteLine("1. Setting Configuration Values\n");

            await configService.SetConfigurationAsync(
                tenantId, "api:rateLimit", "1000", valueType: "int");
            Console.WriteLine("   ✓ api:rateLimit = 1000");

            await configService.SetConfigurationAsync(
                tenantId, "email:sender", "noreply@company.com");
            Console.WriteLine("   ✓ email:sender = noreply@company.com");

            await configService.SetConfigurationAsync(
                tenantId, "features:betaEnabled", "true", valueType: "bool");
            Console.WriteLine("   ✓ features:betaEnabled = true");

            await configService.SetConfigurationAsync(
                tenantId, "billing:currency", "USD");
            Console.WriteLine("   ✓ billing:currency = USD\n");

            // Retrieve configuration with type safety
            Console.WriteLine("2. Retrieving Configuration Values\n");

            var rateLimit = await configService.GetConfigurationAsync<int>(
                tenantId, "api:rateLimit", defaultValue: 100);
            Console.WriteLine($"   api:rateLimit (int): {rateLimit}");

            var emailSender = await configService.GetConfigurationAsync<string>(
                tenantId, "email:sender", defaultValue: "admin@default.com");
            Console.WriteLine($"   email:sender (string): {emailSender}");

            var betaEnabled = await configService.GetConfigurationAsync<bool>(
                tenantId, "features:betaEnabled", defaultValue: false);
            Console.WriteLine($"   features:betaEnabled (bool): {betaEnabled}");

            var currency = await configService.GetConfigurationAsync<string>(
                tenantId, "billing:currency", defaultValue: "USD");
            Console.WriteLine($"   billing:currency (string): {currency}\n");

            // Get all configurations
            Console.WriteLine("3. Retrieving All Configurations\n");

            var allConfigs = await configService.GetAllConfigurationsAsync(tenantId);
            Console.WriteLine($"   Total configurations: {allConfigs.Count}");

            foreach (var kvp in allConfigs.Take(5))
            {
                Console.WriteLine($"   - {kvp.Key} = {kvp.Value}");
            }
            Console.WriteLine();

            // Export configuration
            Console.WriteLine("4. Export Configuration (JSON)\n");

            var json = await configService.ExportConfigurationAsync(tenantId);
            Console.WriteLine("   Exported JSON:");
            Console.WriteLine("   " + json.Replace("\n", "\n   ") + "\n");

            // Import configuration
            Console.WriteLine("5. Import Configuration\n");

            var newTenantId = Guid.NewGuid();
            await configService.ImportConfigurationAsync(newTenantId, json, overwrite: true);
            Console.WriteLine($"   ✓ Imported configuration to tenant: {newTenantId}\n");

            // Delete configuration
            Console.WriteLine("6. Delete Configuration\n");

            var deleted = await configService.DeleteConfigurationAsync(
                tenantId, "features:betaEnabled");
            Console.WriteLine($"   ✓ Deleted features:betaEnabled: {deleted}\n");

            Console.WriteLine("Configuration Hierarchy Examples:");
            Console.WriteLine("┌────────────────────────────────────────────────────┐");
            Console.WriteLine("│ api:rateLimit              → API rate limit        │");
            Console.WriteLine("│ email:sender               → Email sender address  │");
            Console.WriteLine("│ email:template:welcome     → Welcome email body    │");
            Console.WriteLine("│ features:beta:enabled      → Beta feature flag     │");
            Console.WriteLine("│ billing:currency           → Billing currency     │");
            Console.WriteLine("│ stripe:publishable-key     → Stripe API key       │");
            Console.WriteLine("└────────────────────────────────────────────────────┘\n");

            Console.WriteLine("=== Example Complete ===");
        }
    }
}
