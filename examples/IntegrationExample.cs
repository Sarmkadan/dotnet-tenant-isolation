using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Configuration;

namespace TenantIsolation.Examples;

/// <summary>
/// Demonstrates how to integrate the framework into ASP.NET Core DI.
/// </summary>
public static class IntegrationExample
{
    public static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        // 1. Register framework services with default configuration
        services.AddTenantIsolationPhase2Services(options =>
        {
            options.EnableCaching = true;
            options.EnableAuditLogging = true;
            options.EnableBackgroundTasks = true;
        });

        // 2. Register specific data stores (example)
        // In a real app, this would use AddDbContext, etc.
        Console.WriteLine("Services configured with tenant isolation framework.");
    }
}
