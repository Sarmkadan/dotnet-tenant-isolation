// existing content ...

## IHttpClientFactory

The `IHttpClientFactory` provides a centralized way to create and manage HTTP clients with consistent configuration, including timeouts, headers, and authentication. It supports creating authenticated clients, reusing named clients, and configuring advanced options like connection pooling and retries.

### Example Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using TenantIsolation.Integration;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        services.AddTenantIsolationHttpClientFactory();
        services.AddLogging();
        services.AddHttpContextAccessor();
        var serviceProvider = services.BuildServiceProvider();

        // Resolve HTTP client factory
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Create a standard client with base URL
        var client = httpClientFactory.CreateClient("api-client", "https://api.example.com")
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithHeader("X-Api-Version", "2.0");

        // Create an authenticated client
        var authClient = httpClientFactory.CreateAuthenticatedClient("auth-client", "https://auth.example.com", "my-token")
            .WithAccept("application/vnd.example.v1+json");

        // Get a named client (reused across calls)
        var namedClient = httpClientFactory.GetNamedClient("shared-client");
    }
}
```

This example demonstrates:
1. Registering the HTTP client factory with DI
2. Creating standard and authenticated clients
3. Using extension methods to configure timeouts, headers, and content negotiation
4. Reusing named clients for shared configurations