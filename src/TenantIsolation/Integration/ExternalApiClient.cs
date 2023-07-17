// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace TenantIsolation.Integration;

/// <summary>
/// External API response wrapper
/// </summary>
public class ApiCallResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int? HttpStatusCode { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// External API client for integrating with external services
/// Provides standardized HTTP communication with error handling and retry logic
/// </summary>
public interface IExternalApiClient
{
    /// <summary>
    /// Make GET request to external API
    /// </summary>
    Task<ApiCallResult<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null);

    /// <summary>
    /// Make POST request to external API
    /// </summary>
    Task<ApiCallResult<T>> PostAsync<T>(string url, object payload, Dictionary<string, string>? headers = null);

    /// <summary>
    /// Make PUT request to external API
    /// </summary>
    Task<ApiCallResult<T>> PutAsync<T>(string url, object payload, Dictionary<string, string>? headers = null);

    /// <summary>
    /// Make DELETE request to external API
    /// </summary>
    Task<ApiCallResult<bool>> DeleteAsync(string url, Dictionary<string, string>? headers = null);
}

/// <summary>
/// External API client implementation
/// </summary>
public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiCallResult<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null)
    {
        return await MakeRequestAsync<T>(HttpMethod.Get, url, null, headers);
    }

    public async Task<ApiCallResult<T>> PostAsync<T>(string url, object payload, Dictionary<string, string>? headers = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await MakeRequestAsync<T>(HttpMethod.Post, url, content, headers);
    }

    public async Task<ApiCallResult<T>> PutAsync<T>(string url, object payload, Dictionary<string, string>? headers = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await MakeRequestAsync<T>(HttpMethod.Put, url, content, headers);
    }

    public async Task<ApiCallResult<bool>> DeleteAsync(string url, Dictionary<string, string>? headers = null)
    {
        var result = await MakeRequestAsync<object>(HttpMethod.Delete, url, null, headers);
        return new ApiCallResult<bool>
        {
            IsSuccess = result.IsSuccess,
            Data = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
            HttpStatusCode = result.HttpStatusCode,
            Duration = result.Duration
        };
    }

    /// <summary>
    /// Make HTTP request with retry logic and timeout handling
    /// </summary>
    private async Task<ApiCallResult<T>> MakeRequestAsync<T>(
        HttpMethod method,
        string url,
        HttpContent? content,
        Dictionary<string, string>? headers,
        int retryCount = 0,
        int maxRetries = 3)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(method, url);

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (content != null)
                request.Content = content;

            _logger.LogInformation("Making external API call: {Method} {Url}", method, url);

            var response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(jsonContent);

                _logger.LogInformation("External API call successful. Duration: {Duration}ms",
                    stopwatch.ElapsedMilliseconds);

                return new ApiCallResult<T>
                {
                    IsSuccess = true,
                    Data = data,
                    HttpStatusCode = (int)response.StatusCode,
                    Duration = stopwatch.Elapsed
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("External API call failed with status {StatusCode}. Response: {Response}",
                response.StatusCode, errorContent);

            return new ApiCallResult<T>
            {
                IsSuccess = false,
                ErrorMessage = $"API returned {response.StatusCode}",
                HttpStatusCode = (int)response.StatusCode,
                Duration = stopwatch.Elapsed
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();

            // Retry on transient errors
            if (retryCount < maxRetries && IsTransientError(ex))
            {
                _logger.LogWarning(ex, "Transient error in external API call. Retrying ({Attempt}/{MaxRetries})",
                    retryCount + 1, maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                return await MakeRequestAsync<T>(method, url, content, headers, retryCount + 1, maxRetries);
            }

            _logger.LogError(ex, "Error making external API call to {Url}", url);

            return new ApiCallResult<T>
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Unexpected error in external API call");

            return new ApiCallResult<T>
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Check if error is transient and should be retried
    /// </summary>
    private bool IsTransientError(HttpRequestException ex)
    {
        // Retry on connection errors and timeouts
        return ex.InnerException is TimeoutException ||
               ex.InnerException is IOException ||
               ex.InnerException is HttpRequestException;
    }
}

/// <summary>
/// Extension method to register external API client
/// </summary>
public static class ExternalApiClientExtensions
{
    public static IServiceCollection AddExternalApiClient(this IServiceCollection services)
    {
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "TenantIsolation/1.0");
            });

        return services;
    }
}
