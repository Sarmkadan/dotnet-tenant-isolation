using TenantIsolation.Services;
using TenantIsolation.Models;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Examples;

/// <summary>
/// Demonstrates advanced configuration, error handling, and feature toggling.
/// </summary>
public class AdvancedUsage
{
    private readonly ConfigurationService _configService;
    private readonly TenantFeatureService _featureService;

    public AdvancedUsage(ConfigurationService configService, TenantFeatureService featureService)
    {
        _configService = configService;
        _featureService = featureService;
    }

    public async Task RunAdvancedExampleAsync(Guid tenantId)
    {
        try
        {
            // 1. Configure per-tenant setting
            await _configService.SetConfigurationAsync(
                tenantId, "features:maxUsers", "100", valueType: "int");
            
            var maxUsers = await _configService.GetConfigurationAsync<int>(
                tenantId, "features:maxUsers", defaultValue: 50);
            
            Console.WriteLine($"Configured MaxUsers: {maxUsers}");

            // 2. Manage feature toggle
            await _featureService.EnableFeatureAsync(tenantId, "premium-dashboard");
            
            var isEnabled = await _featureService.IsFeatureEnabledAsync(tenantId, "premium-dashboard");
            Console.WriteLine($"Is 'premium-dashboard' enabled? {isEnabled}");

        }
        catch (TenantIsolationException ex)
        {
            Console.WriteLine($"Handled expected isolation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
