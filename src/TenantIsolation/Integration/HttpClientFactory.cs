// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Integration;

/// <summary>
/// Factory for creating configured HTTP clients
/// Provides resilience, timeouts, and automatic header injection
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Create HTTP client for external API calls
    /// </summary>
    HttpClient CreateClient(string clientName, string? baseUrl = null);

    /// <summary>
    /// Create HTTP client with authentication
    /// </summary>
    HttpClient CreateAuthenticatedClient(string clientName, string? baseUrl, string token);

    /// <summary>
    /// Get or create named client
    /// </summary>
    HttpClient GetNamedClient(string clientName);
}

/// <summary>
/// HTTP client factory implementation
/// Creates pre-configured HTTP clients with standard settings
/// </summary>
public class TenantIsolationHttpClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantIsolationHttpClientFactory> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, HttpClient> _clients = new();

    public TenantIsolationHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<TenantIsolationHttpClientFactory> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public HttpClient CreateClient(string clientName, string? baseUrl = null)
    {
        var client = _httpClientFactory.CreateClient(clientName);

        // Set standard headers
        client.DefaultRequestHeaders.Add("User-Agent", "TenantIsolation/1.0");

        // Inject correlation ID from current request
        if (_httpContextAccessor.HttpContext?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
            client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId?.ToString() ?? string.Empty);

        // Set timeout
        client.Timeout = TimeSpan.FromSeconds(30);

        if (!string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);

        _logger.LogInformation("Created HTTP client '{ClientName}' with base address '{BaseAddress}'",
            clientName, baseUrl ?? "none");

        return client;
    }

    public HttpClient CreateAuthenticatedClient(string clientName, string? baseUrl, string token)
    {
        var client = CreateClient(clientName, baseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient GetNamedClient(string clientName)
    {
        if (_clients.TryGetValue(clientName, out var existingClient))
            return existingClient;

        var client = CreateClient(clientName);
        _clients[clientName] = client;
        return client;
    }
}

/// <summary>
/// Options for HTTP client configuration
/// </summary>
public class HttpClientOptions
{
    /// <summary>
    /// Default request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable automatic decompression
    /// </summary>
    public bool AutomaticDecompression { get; set; } = true;

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 10;
}

/// <summary>
/// Extension methods for HTTP client configuration
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Add response header value
    /// </summary>
    public static HttpClient WithHeader(this HttpClient client, string name, string value)
    {
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            client.DefaultRequestHeaders.Add(name, value);
        return client;
    }

    /// <summary>
    /// Set Accept header
    /// </summary>
    public static HttpClient WithAccept(this HttpClient client, string mediaType = "application/json")
    {
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(mediaType));
        return client;
    }

    /// <summary>
    /// Set User-Agent header
    /// </summary>
    public static HttpClient WithUserAgent(this HttpClient client, string userAgent)
    {
        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        return client;
    }

    /// <summary>
    /// Set Bearer token authorization
    /// </summary>
    public static HttpClient WithBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Set request timeout
    /// </summary>
    public static HttpClient WithTimeout(this HttpClient client, TimeSpan timeout)
    {
        client.Timeout = timeout;
        return client;
    }
}

/// <summary>
/// Extension method to register HTTP client factory
/// </summary>
public static class HttpClientFactoryExtensions
{
    public static IServiceCollection AddTenantIsolationHttpClientFactory(this IServiceCollection services)
    {
        services.AddScoped<IHttpClientFactory, TenantIsolationHttpClientFactory>();
        services.AddHttpClient();
        return services;
    }
}
