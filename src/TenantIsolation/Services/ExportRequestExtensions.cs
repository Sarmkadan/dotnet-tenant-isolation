#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace TenantIsolation.Services;

/// <summary>
/// Extension methods for <see cref="ExportRequest"/> to provide common operations and validations.
/// </summary>
public static class ExportRequestExtensions
{
    /// <summary>
    /// Validates that the <see cref="ExportRequest"/> has required properties set.
    /// </summary>
    /// <param name="request">The export request to validate.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public static bool IsValid(this ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return !string.IsNullOrWhiteSpace(request.ResourceType) &&
               request.TenantId != Guid.Empty;
    }

    /// <summary>
    /// Gets the default filename for this export request.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <returns>Generated filename with format: {ResourceType}_{Timestamp}.{extension}.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public static string GetFileName(this ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var extension = request.Format switch
        {
            ExportFormat.Json => "json",
            ExportFormat.Csv => "csv",
            ExportFormat.Xml => "xml",
            _ => "txt"
        };

        return $"{request.ResourceType}_{timestamp}.{extension}";
    }

    /// <summary>
    /// Gets the content type for the export format.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <returns>Content type string based on format.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public static string GetContentType(this ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Format switch
        {
            ExportFormat.Json => "application/json",
            ExportFormat.Csv => "text/csv",
            ExportFormat.Xml => "application/xml",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Creates a dictionary of export options from the request.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <returns>Dictionary containing export configuration options.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public static Dictionary<string, object> GetExportOptions(this ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new Dictionary<string, object>
        {
            ["ResourceType"] = request.ResourceType,
            ["Format"] = request.Format,
            ["TenantId"] = request.TenantId
        };

        if (request.Filters?.Count > 0)
        {
            options["Filters"] = request.Filters;
        }

        if (request.IncludeFields?.Count > 0)
        {
            options["IncludeFields"] = request.IncludeFields;
        }

        return options;
    }

    /// <summary>
    /// Checks if the specified field should be included in the export.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <param name="fieldName">Name of the field to check.</param>
    /// <returns>True if field should be included or if IncludeFields is null/empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> or <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="fieldName"/> is whitespace.</exception>
    public static bool ShouldIncludeField(this ExportRequest request, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        return request.IncludeFields is null or { Count: 0 } ||
               request.IncludeFields.Contains(fieldName);
    }
}