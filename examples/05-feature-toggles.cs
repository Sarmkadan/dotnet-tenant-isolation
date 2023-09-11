// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 5: Feature Toggles and Rollouts
/// Demonstrates feature flag management with gradual rollouts.
/// </summary>
public class FeatureTogglesExample
{
    public static async Task RunAsync(WebApplication app, Guid tenantId)
    {
        using (var scope = app.Services.CreateAsyncScope())
        {
            var featureService = scope.ServiceProvider
                .GetRequiredService<TenantFeatureService>();

            Console.WriteLine("=== Feature Toggles Example ===\n");

            // Check feature status
            Console.WriteLine("1. Checking Feature Status\n");

            var isAnalyticsEnabled = await featureService.IsFeatureEnabledAsync(
                tenantId, "advanced-analytics");
            Console.WriteLine($"   advanced-analytics: {isAnalyticsEnabled}");

            var isDarkModeEnabled = await featureService.IsFeatureEnabledAsync(
                tenantId, "dark-mode");
            Console.WriteLine($"   dark-mode: {isDarkModeEnabled}\n");

            // Enable feature for tenant
            Console.WriteLine("2. Enabling Features\n");

            await featureService.EnableFeatureAsync(tenantId, "advanced-analytics");
            Console.WriteLine("   ✓ Enabled advanced-analytics");

            await featureService.EnableFeatureAsync(tenantId, "dark-mode");
            Console.WriteLine("   ✓ Enabled dark-mode\n");

            // Verify enabling worked
            var analyticsEnabled = await featureService.IsFeatureEnabledAsync(
                tenantId, "advanced-analytics");
            Console.WriteLine($"   Verification - advanced-analytics: {analyticsEnabled}\n");

            // Gradual rollout strategy
            Console.WriteLine("3. Gradual Feature Rollout (Canary Deployment)\n");

            Console.WriteLine("   Week 1: Rollout to 5% of users");
            await featureService.SetRolloutPercentageAsync(
                tenantId, "experimental-ui", 5);
            Console.WriteLine("   ✓ Set rollout to 5%\n");

            Console.WriteLine("   Week 2: Increase to 25%");
            await featureService.SetRolloutPercentageAsync(
                tenantId, "experimental-ui", 25);
            Console.WriteLine("   ✓ Set rollout to 25%\n");

            Console.WriteLine("   Week 3: Increase to 50%");
            await featureService.SetRolloutPercentageAsync(
                tenantId, "experimental-ui", 50);
            Console.WriteLine("   ✓ Set rollout to 50%\n");

            Console.WriteLine("   Week 4: Full rollout");
            await featureService.SetRolloutPercentageAsync(
                tenantId, "experimental-ui", 100);
            Console.WriteLine("   ✓ Set rollout to 100%\n");

            // Record feature usage
            Console.WriteLine("4. Recording Feature Usage\n");

            for (int i = 0; i < 10; i++)
            {
                await featureService.RecordFeatureUsageAsync(tenantId, "dark-mode");
            }
            Console.WriteLine("   ✓ Recorded 10 usage events for dark-mode\n");

            // Get feature statistics
            Console.WriteLine("5. Feature Statistics\n");

            var stats = await featureService.GetStatisticsAsync(tenantId);
            Console.WriteLine($"   Enabled features: {stats.EnabledCount}");
            Console.WriteLine($"   Disabled features: {stats.DisabledCount}");
            Console.WriteLine($"   Total usage events: {stats.TotalUsage}\n");

            // Disable a feature
            Console.WriteLine("6. Disabling Features\n");

            await featureService.DisableFeatureAsync(tenantId, "dark-mode");
            Console.WriteLine("   ✓ Disabled dark-mode\n");

            // Feature toggle patterns
            Console.WriteLine("Feature Toggle Patterns:");
            Console.WriteLine("┌───────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Pattern              │ Use Case                      │");
            Console.WriteLine("├───────────────────────────────────────────────────────┤");
            Console.WriteLine("│ Kill Switch          │ Disable broken features       │");
            Console.WriteLine("│ Canary Deployment    │ Gradual rollout              │");
            Console.WriteLine("│ A/B Testing          │ Test two versions            │");
            Console.WriteLine("│ Beta Program         │ Opt-in early access          │");
            Console.WriteLine("│ Permission-Based     │ Feature access control       │");
            Console.WriteLine("│ Deprecation          │ Retire old features          │");
            Console.WriteLine("└───────────────────────────────────────────────────────┘\n");

            Console.WriteLine("Rollout Percentage Behavior:");
            Console.WriteLine("  0%    → Feature disabled for all users");
            Console.WriteLine("  25%   → Feature available to ~25% of requests");
            Console.WriteLine("  50%   → Feature available to ~50% of requests");
            Console.WriteLine("  100%  → Feature enabled for all users\n");

            Console.WriteLine("=== Example Complete ===");
        }
    }
}
