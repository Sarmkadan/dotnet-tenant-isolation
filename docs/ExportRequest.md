# ExportRequest

The `ExportRequest` type serves as the primary contract for defining data export operations within the `dotnet-tenant-isolation` framework. It encapsulates the necessary metadata to identify the target tenant, specify the resource type, and configure the desired output format and filtering criteria. Additionally, this type includes members representing the resulting export payload and provides static and instance methods for executing exports, querying supported formats, and registering the underlying `ExportService` within the dependency injection container.

## API

### Properties

#### `TenantId`
*   **Type:** `Guid`
*   **Purpose:** Identifies the specific tenant associated with the export request, ensuring data isolation boundaries are respected during operation.
*   **Remarks:** This property appears twice in the definition scope; it represents the same logical identifier for the tenant context.

#### `ResourceType`
*   **Type:** `string`
*   **Purpose:** Specifies the category or entity type of the data to be exported (e.g., "Users", "Transactions", "Logs").

#### `Format`
*   **Type:** `ExportFormat`
*   **Purpose:** Defines the serialization format for the exported data (e.g., JSON, CSV, XML).

#### `Filters`
*   **Type:** `Dictionary<string, object>?`
*   **Purpose:** Contains optional key-value pairs used to narrow down the dataset included in the export.
*   **Remarks:** May be `null` if no filtering is required.

#### `IncludeFields`
*   **Type:** `List<string>?`
*   **Purpose:** An optional list of specific field names to include in the export, allowing for projection of only necessary data columns.
*   **Remarks:** May be `null` to indicate that all available fields for the `ResourceType` should be included.

#### `Id`
*   **Type:** `string`
*   **Purpose:** A unique identifier string for the specific export request instance, used for tracking and correlation.

#### `Content`
*   **Type:** `byte[]`
*   **Purpose:** Holds the raw binary data of the completed export operation.
*   **Remarks:** Typically populated after the export execution completes.

#### `ContentType`
*   **Type:** `string`
*   **Purpose:** Indicates the MIME type of the exported `Content` (e.g., "application/json", "text/csv").

#### `FileName`
*   **Type:** `string`
*   **Purpose:** Suggests the default file name for the exported data, including the appropriate extension.

#### `CreatedAt`
*   **Type:** `DateTime`
*   **Purpose:** Records the timestamp when the export result was generated.

#### `SizeBytes`
*   **Type:** `long`
*   **Purpose:** Represents the size of the exported `Content` in bytes.

#### `ExportService`
*   **Type:** `ExportService`
*   **Purpose:** Provides access to the underlying service instance responsible for processing export logic.

### Methods

#### `ExportAsync`
*   **Signature:** `public async Task<ExportResult> ExportAsync`
*   **Purpose:** Asynchronously executes the export operation based on the current configuration properties (`TenantId`, `ResourceType`, `Filters`, etc.).
*   **Return Value:** Returns a `Task<ExportResult>` containing the outcome of the operation, including the generated data stream and metadata.
*   **Exceptions:** May throw exceptions related to tenant access violations, unsupported resource types, or serialization failures depending on the `ExportService` implementation.

#### `GetSupportedFormats`
*   **Signature:** `public IEnumerable<ExportFormat> GetSupportedFormats`
*   **Purpose:** Retrieves a collection of export formats that are currently supported by the configured service for the specified `ResourceType`.
*   **Return Value:** An `IEnumerable<ExportFormat>` listing available formats.
*   **Remarks:** Does not modify state; safe for read-only inspection.

#### `AddExportService`
*   **Signature:** `public static IServiceCollection AddExportService`
*   **Purpose:** Registers the `ExportService` and its dependencies into the provided `IServiceCollection` for dependency injection.
*   **Parameters:** Implicitly requires an `IServiceCollection` instance (standard extension method pattern).
*   **Return Value:** Returns the `IServiceCollection` to allow method chaining.
*   **Remarks:** Should be called during application startup configuration.

## Usage

### Example 1: Configuring and Executing an Export
This example demonstrates instantiating an `ExportRequest`, configuring filters for a specific tenant, and executing the export asynchronously.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetTenantIsolation;

public class ExportOrchestrator
{
    private readonly ExportService _exportService;

    public ExportOrchestrator(ExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task<byte[]> DownloadTenantDataAsync(Guid tenantId)
    {
        var request = new ExportRequest
        {
            TenantId = tenantId,
            ResourceType = "Invoices",
            Format = ExportFormat.Csv,
            Filters = new Dictionary<string, object>
            {
                { "Status", "Paid" },
                { "Year", 2023 }
            },
            IncludeFields = new List<string> { "InvoiceId", "Amount", "Date" }
        };

        // Attach the service instance if not automatically bound
        request.ExportService = _exportService;

        var result = await request.ExportAsync();
        
        return result.Content;
    }
}
```

### Example 2: Service Registration and Format Validation
This example shows how to register the service in the DI container and validate supported formats before initiating a request.

```csharp
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using DotnetTenantIsolation;

public class StartupConfig
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register the export service
        ExportRequest.AddExportService(services);
    }

    public void ValidateFormat(ExportRequest request)
    {
        var supported = request.GetSupportedFormats();
        
        if (!supported.Contains(request.Format))
        {
            var available = string.Join(", ", supported.Select(f => f.ToString()));
            throw new InvalidOperationException(
                $"Format '{request.Format}' is not supported. Available formats: {available}");
        }
    }
}
```

## Notes

*   **Thread Safety:** The `ExportRequest` instance contains mutable state (e.g., `Content`, `Filters`). While `GetSupportedFormats` is read-only, `ExportAsync` modifies the internal state to populate result properties (`Content`, `SizeBytes`, etc.). Instances should not be shared across concurrent threads without external synchronization. A new instance should be created for each distinct export operation.
*   **Nullability:** `Filters` and `IncludeFields` are nullable. Implementations consuming this type must handle `null` values gracefully, interpreting them as "no filters applied" or "include all fields," respectively.
*   **Tenant Isolation:** The `TenantId` property is critical for security. The underlying `ExportService` must validate that the executing context has permission to access data for the specified `TenantId`. Failure to enforce this at the service level could lead to data leakage between tenants.
*   **Memory Usage:** The `Content` property stores the entire export payload as a `byte[]`. For very large datasets, this may lead to high memory pressure. Consumers should monitor `SizeBytes` and consider streaming alternatives if the payload exceeds available memory thresholds.
*   **Duplicate Definitions:** The type definition lists `TenantId`, `ResourceType`, and `Format` multiple times in the public member list. In practice, these represent single properties; redundant declarations in documentation sources should be treated as a single logical member per name.
