# ExportRequestExtensions

Provides a set of static extension methods for evaluating and extracting information from an export request within the tenant isolation workflow. These helpers centralize validation, naming, content type determination, option retrieval, and field inclusion logic, allowing callers to keep export‑related code concise and consistent.

## API

### IsValid
```csharp
public static bool IsValid(this ExportRequest request)
```
**Purpose** – Determines whether the supplied export request contains all required data and conforms to business rules.  
**Parameters**  
- `request`: The export request to validate.  
**Return value** – `true` if the request is valid; otherwise `false`.  
**Exceptions** – Throws `ArgumentNullException` if `request` is `null`.

### GetFileName
```csharp
public static string GetFileName(this ExportRequest request)
```
**Purpose** – Generates a file name appropriate for the export based on request metadata (e.g., tenant identifier, timestamp, export type).  
**Parameters**  
- `request`: The export request providing contextual data.  
**Return value** – A string representing the file name, without path.  
**Exceptions** – Throws `ArgumentNullException` if `request` is `null`. May throw `InvalidOperationException` if required fields for name generation are missing or malformed.

### GetContentType
```csharp
public static string GetContentType(this ExportRequest request)
```
**Purpose** – Returns the MIME content type that should be used when serving the exported file.  
**Parameters**  
- `request`: The export request indicating the desired format.  
**Return value** – A content‑type string such as `"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"` or `"text/csv"`.  
**Exceptions** – Throws `ArgumentNullException` if `request` is `null`. Throws `NotSupportedException` if the request specifies an unsupported export format.

### GetExportOptions
```csharp
public static Dictionary<string, object> GetExportOptions(this ExportRequest request)
```
**Purpose** – Retrieves a dictionary of additional options that influence the export process (e.g., column delimiters, formatting flags).  
**Parameters**  
- `request`: The export request containing optional settings.  
**Return value** – A read‑only dictionary where keys are option names and values are their corresponding settings. Returns an empty dictionary if no options are present.  
**Exceptions** – Throws `ArgumentNullException` if `request` is `null`.  

### ShouldIncludeField
```csharp
public static bool ShouldIncludeField(this ExportRequest request, string fieldName)
```
**Purpose** – Determines whether a specific field should be included in the exported output based on request‑level inclusion/exclusion rules.  
**Parameters**  
- `request`: The export request governing field visibility.  
- `fieldName`: The name of the field to evaluate.  
**Return value** – `true` if the field should be exported; otherwise `false`.  
**Exceptions** – Throws `ArgumentNullException` if `request` or `fieldName` is `null`. Throws `ArgumentException` if `fieldName` is empty or whitespace.

## Usage

```csharp
var request = new ExportRequest
{
    TenantId = "contoso",
    ExportType = ExportType.Excel,
    IncludeFields = new[] { "Id", "Name", "Email" }
};

if (!request.IsValid())
{
    throw new InvalidOperationException("Export request is missing required data.");
}

string fileName = request.GetFileName();          // e.g., "contoso_export_20251102.xlsx"
string contentType = request.GetContentType();   // "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
var options = request.GetExportOptions();        // possibly contains { "HeaderStyle": "Bold" }

foreach (var field in request.AllAvailableFields)
{
    if (request.ShouldIncludeField(field.Name))
    {
        // export field
    }
}
```

```csharp
// Example handling a CSV export with custom delimiter
var csvRequest = new ExportRequest
{
    TenantId = "fabrikam",
    ExportType = ExportType.Csv,
    Options = new Dictionary<string, object> { { "Delimiter", ";" } }
};

string csvFileName = csvRequest.GetFileName();   // "fabrikam_export_20251102.csv"
string csvContentType = csvRequest.GetContentType(); // "text/csv"
var csvOptions = csvRequest.GetExportOptions(); // { "Delimiter": ";" }

if (csvOptions.TryGetValue("Delimiter", out var delim) && delim is string separator)
{
    // use separator when building CSV lines
}
```

## Notes

- All extension methods are pure functions of their input parameters; they do not access or modify any static or instance state. Consequently, they are thread‑safe when called concurrently with distinct `ExportRequest` instances.
- Passing a `null` `ExportRequest` to any member results in an `ArgumentNullException`. Callers should validate the request beforehand if they wish to avoid exceptions.
- `GetFileName` and `GetContentType` rely on the presence of certain properties (e.g., `TenantId`, `ExportType`). If those properties are unset or contain invalid values, the methods may throw `InvalidOperationException` or `NotSupportedException` respectively.
- The dictionary returned by `GetExportOptions` is intended to be treated as immutable; altering the returned dictionary may lead to undefined behavior in downstream export logic.
- `ShouldIncludeField` performs a case‑sensitive comparison of `fieldName` against the request’s inclusion/exclusion lists. Normalizing field names prior to calling the method can prevent mismatches due to casing differences.
