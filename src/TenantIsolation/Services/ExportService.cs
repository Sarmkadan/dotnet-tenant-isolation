#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.IO.Compression;
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
    public int? MaxRecords { get; set; }
    public bool Compress { get; set; }
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
    private static readonly Dictionary<ExportFormat, (string ContentType, string Extension)> FormatMetadata = new()
    {
        [ExportFormat.Json] = ("application/json", "json"),
        [ExportFormat.Csv] = ("text/csv", "csv"),
        [ExportFormat.Xml] = ("application/xml", "xml")
    };

    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public Task<ExportResult> ExportAsync(ExportRequest request, List<object> data)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.ResourceType))
            throw new ArgumentException("Resource type must be provided.", nameof(request));

        if (request.MaxRecords.HasValue && data.Count > request.MaxRecords.Value)
        {
            throw new InvalidOperationException(
                $"Export contains {data.Count} records, which exceeds the maximum of {request.MaxRecords.Value} records.");
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Exporting {Count} {ResourceType} records in {Format} format for tenant {TenantId}",
            data.Count, request.ResourceType, request.Format, request.TenantId);

        if (data.Count == 0)
        {
            _logger.LogWarning("Export data is empty for tenant {TenantId} and resource type {ResourceType}",
                request.TenantId, request.ResourceType);
        }

        byte[] content;
        try
        {
            content = request.Format switch
            {
                ExportFormat.Json => ExportToJson(data, request.IncludeFields),
                ExportFormat.Csv => ExportToCsv(data, request.IncludeFields),
                ExportFormat.Xml => ExportToXml(data, request.ResourceType, request.IncludeFields),
                _ => throw new NotSupportedException($"Format {request.Format} is not supported")
            };

            if (request.Compress)
                content = CompressContent(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Export conversion failed for tenant {TenantId}, resource type {ResourceType}, and format {Format}",
                request.TenantId, request.ResourceType, request.Format);
            throw;
        }

        var result = new ExportResult
        {
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            Format = request.Format,
            Content = content,
            ContentType = request.Compress ? "application/gzip" : GetContentType(request.Format),
            FileName = GenerateFileName(request.ResourceType, request.Format) + (request.Compress ? ".gz" : string.Empty),
            SizeBytes = content.Length
        };

        stopwatch.Stop();
        _logger.LogInformation(
            "Export {ExportId} completed for tenant {TenantId} in {Format} format with {SizeBytes} bytes in {ElapsedMilliseconds} ms",
            result.Id, result.TenantId, result.Format, result.SizeBytes, stopwatch.ElapsedMilliseconds);
        return Task.FromResult(result);
    }

    public IEnumerable<ExportFormat> GetSupportedFormats(string resourceType)
    {
        return new[] { ExportFormat.Json, ExportFormat.Csv, ExportFormat.Xml };
    }

    /// <summary>
    /// Export data to JSON format
    /// </summary>
    private static byte[] ExportToJson(List<object> data, List<string>? includeFields)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Export data to CSV format
    /// </summary>
    private static byte[] ExportToCsv(List<object> data, List<string>? includeFields)
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
    private static byte[] ExportToXml(List<object> data, string rootElementName, List<string>? includeFields)
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

    private static byte[] CompressContent(byte[] content)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(content, 0, content.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Get content type for format
    /// </summary>
    private static string GetContentType(ExportFormat format)
    {
        return FormatMetadata.TryGetValue(format, out var metadata)
            ? metadata.ContentType
            : "application/octet-stream";
    }

    /// <summary>
    /// Generate filename for export
    /// </summary>
    private static string GenerateFileName(string resourceType, ExportFormat format)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var extension = FormatMetadata.TryGetValue(format, out var metadata)
            ? metadata.Extension
            : "txt";

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
