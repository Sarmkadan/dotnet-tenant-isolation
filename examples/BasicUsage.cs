using TenantIsolation.Services;
using TenantIsolation.Models;

namespace TenantIsolation.Examples;

/// <summary>
/// Demonstrates basic tenant creation, activation, and verification.
/// </summary>
public class BasicUsage
{
    private readonly TenantService _tenantService;

    public BasicUsage(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task<Tenant> RunBasicExampleAsync(string name, string slug, string adminEmail)
    {
        // 1. Create a new tenant
        var tenant = await _tenantService.CreateTenantAsync(name, slug, adminEmail);
        Console.WriteLine($"Tenant '{tenant.Name}' created with ID: {tenant.Id}");

        // 2. Activate the tenant
        await _tenantService.ActivateTenantAsync(tenant.Id);
        Console.WriteLine($"Tenant '{tenant.Name}' activated.");

        // 3. Verify subscription
        var isValid = await _tenantService.IsSubscriptionValidAsync(tenant.Id);
        Console.WriteLine($"Is subscription valid? {isValid}");

        return tenant;
    }
}
