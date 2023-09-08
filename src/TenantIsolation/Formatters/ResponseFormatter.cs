// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text.Json.Serialization;

namespace TenantIsolation.Formatters;

/// <summary>
/// Standard API response format wrapper
/// Ensures consistent response structure across all endpoints
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    [JsonPropertyName("traceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }
}

/// <summary>
/// Paginated response for list endpoints
/// </summary>
public class PaginatedResponse<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages => (Total + PageSize - 1) / PageSize;

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Response formatter for consistent API responses
/// Handles success, error, and paginated responses
/// </summary>
public interface IResponseFormatter
{
    /// <summary>
    /// Create success response
    /// </summary>
    ApiResponse<T> Success<T>(T data, string? message = null);

    /// <summary>
    /// Create success response without data
    /// </summary>
    ApiResponse<object?> Success(string? message = null);

    /// <summary>
    /// Create error response
    /// </summary>
    ApiResponse<object?> Error(string message, Dictionary<string, string[]>? errors = null);

    /// <summary>
    /// Create paginated response
    /// </summary>
    ApiResponse<PaginatedResponse<T>> Paginated<T>(
        List<T> items,
        int total,
        int page,
        int pageSize,
        string? message = null);
}

/// <summary>
/// Implementation of response formatter
/// </summary>
public class ResponseFormatter : IResponseFormatter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResponseFormatter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ApiResponse<T> Success<T>(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully",
            Path = _httpContextAccessor.HttpContext?.Request.Path.ToString(),
            TraceId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };
    }

    public ApiResponse<object?> Success(string? message = null)
    {
        return new ApiResponse<object?>
        {
            Success = true,
            Data = null,
            Message = message ?? "Operation completed successfully",
            Path = _httpContextAccessor.HttpContext?.Request.Path.ToString(),
            TraceId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };
    }

    public ApiResponse<object?> Error(string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResponse<object?>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Path = _httpContextAccessor.HttpContext?.Request.Path.ToString(),
            TraceId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };
    }

    public ApiResponse<PaginatedResponse<T>> Paginated<T>(
        List<T> items,
        int total,
        int page,
        int pageSize,
        string? message = null)
    {
        var paginatedData = new PaginatedResponse<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return new ApiResponse<PaginatedResponse<T>>
        {
            Success = true,
            Data = paginatedData,
            Message = message ?? "Items retrieved successfully",
            Path = _httpContextAccessor.HttpContext?.Request.Path.ToString(),
            TraceId = _httpContextAccessor.HttpContext?.TraceIdentifier
        };
    }
}

/// <summary>
/// Extension method to register response formatter
/// </summary>
public static class ResponseFormatterExtensions
{
    public static IServiceCollection AddResponseFormatter(this IServiceCollection services)
    {
        services.AddScoped<IResponseFormatter, ResponseFormatter>();
        return services;
    }
}
