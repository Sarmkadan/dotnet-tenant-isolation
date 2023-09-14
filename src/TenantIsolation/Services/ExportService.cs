#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Services;

/// <summary>
/// Export format types
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Xml
}

/// <summary>
/// Export request
/// </summary>
public class ExportRequest
{
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public ExportFormat Format { get; set; } = ExportFormat.Json;
    public Dictionary<string, object>? Filters { get; set; }
    public List<string>? IncludeFields { get; set; }
}

/// <summary>
/// Export result
/// </summary>
public class ExportResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long SizeBytes { get; set; }
}

/// <summary>
/// Export service interface
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export data to specified format
    /// </summary>
    Task<ExportResult> ExportAsync(ExportRequest request, List<object> data);

    /// <summary>
    /// Get supported formats for resource type
    /// </summary>
    IEnumerable<ExportFormat> GetSupportedFormats(string resourceType);
}

/// <summary>
/// Export service implementation
/// Handles conversion of data to various export formats
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsync(ExportRequest request, List<object> data)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Exporting {Count} {ResourceType} records in {Format} format for tenant {TenantId}",
            data.Count, request.ResourceType, request.Format, request.TenantId);

        var content = request.Format switch
        {
            ExportFormat.Json => ExportToJson(data, request.IncludeFields),
            ExportFormat.Csv => ExportToCsv(data, request.IncludeFields),
            ExportFormat.Xml => ExportToXml(data, request.ResourceType, request.IncludeFields),
            _ => throw new NotSupportedException($"Format {request.Format} is not supported")
        };

        var result = new ExportResult
        {
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            Format = request.Format,
            Content = content,
            ContentType = GetContentType(request.Format),
            FileName = GenerateFileName(request.ResourceType, request.Format),
            SizeBytes = content.Length
        };

        _logger.LogInformation("Export completed. File size: {SizeBytes} bytes", result.SizeBytes);
        return await Task.FromResult(result);
    }

    public IEnumerable<ExportFormat> GetSupportedFormats(string resourceType)
    {
        return new[] { ExportFormat.Json, ExportFormat.Csv, ExportFormat.Xml };
    }

    /// <summary>
    /// Export data to JSON format
    /// </summary>
    private byte[] ExportToJson(List<object> data, List<string>? includeFields)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Export data to CSV format
    /// </summary>
    private byte[] ExportToCsv(List<object> data, List<string>? includeFields)
    {
        if (data.Count == 0)
            return Array.Empty<byte>();

        var csv = new StringBuilder();
        var firstItem = data[0];

        // Get properties
        var properties = firstItem.GetType().GetProperties();
        var fieldsToExport = includeFields != null
            ? properties.Where(p => includeFields.Contains(p.Name)).ToList()
            : properties.ToList();

        // Write header
        var header = string.Join(",", fieldsToExport.Select(p => EscapeCsvField(p.Name)));
        csv.AppendLine(header);

        // Write rows
        foreach (var item in data)
        {
            var values = fieldsToExport.Select(p => EscapeCsvField(p.GetValue(item)?.ToString() ?? ""));
            csv.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Export data to XML format
    /// </summary>
    private byte[] ExportToXml(List<object> data, string rootElementName, List<string>? includeFields)
    {
        var root = new XElement(rootElementName);

        foreach (var item in data)
        {
            var itemElement = new XElement("item");
            var properties = item.GetType().GetProperties();

            var fieldsToExport = includeFields != null
                ? properties.Where(p => includeFields.Contains(p.Name))
                : properties;

            foreach (var prop in fieldsToExport)
            {
                var value = prop.GetValue(item);
                itemElement.Add(new XElement(prop.Name, value?.ToString() ?? ""));
            }

            root.Add(itemElement);
        }

        var doc = new XDocument(root);
        var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Escape CSV field (quote if contains comma or quote)
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }

    /// <summary>
    /// Get content type for format
    /// </summary>
    private string GetContentType(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => "application/json",
            ExportFormat.Csv => "text/csv",
            ExportFormat.Xml => "application/xml",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Generate filename for export
    /// </summary>
    private string GenerateFileName(string resourceType, ExportFormat format)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var extension = format switch
        {
            ExportFormat.Json => "json",
            ExportFormat.Csv => "csv",
            ExportFormat.Xml => "xml",
            _ => "txt"
        };

        return $"{resourceType}_{timestamp}.{extension}";
    }
}

/// <summary>
/// Extension method to register export service
/// </summary>
public static class ExportServiceExtensions
{
    public static IServiceCollection AddExportService(this IServiceCollection services)
    {
        services.AddScoped<IExportService, ExportService>();
        return services;
    }
}
