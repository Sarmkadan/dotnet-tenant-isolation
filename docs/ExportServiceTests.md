# ExportServiceTests

Unit tests for the `ExportService` class, verifying correct behavior across different output formats (JSON, CSV, XML) and edge cases like null requests.

## API

### `ExportServiceTests()`
Constructor for the test class. Initializes test dependencies and required test context.

### `Task ExportAsync_WithNullRequest_ThrowsArgumentNullException()`
Verifies that `ExportService.ExportAsync` throws an `ArgumentNullException` when passed a null request.

- **Parameters**: None (test method)
- **Return value**: `Task` (asynchronous test)
- **Throws**: `ArgumentNullException` if the request parameter is null

### `Task ExportAsync_JsonFormat_ReturnsValidJson()`
Ensures that exporting data in JSON format produces valid, well-formed JSON output.

- **Parameters**: None (test method)
- **Return value**: `Task` (asynchronous test)
- **Throws**: Any exception thrown during export or validation of JSON output

### `Task ExportAsync_CsvFormat_ReturnsValidCsv()`
Confirms that exporting data in CSV format generates valid CSV output with correct headers and data rows.

- **Parameters**: None (test method)
- **Return value**: `Task` (asynchronous test)
- **Throws**: Any exception thrown during export or validation of CSV output

### `Task ExportAsync_XmlFormat_ReturnsValidXml()`
Validates that exporting data in XML format results in well-formed XML output.

- **Parameters**: None (test method)
- **Return value**: `Task` (asynchronous test)
- **Throws**: Any exception thrown during export or validation of XML output

### `void GetSupportedFormats_ReturnsAllFormats()`
Checks that the `ExportService.GetSupportedFormats` method returns all expected output formats.

- **Parameters**: None (test method)
- **Return value**: `void` (synchronous test)
- **Throws**: Any exception thrown during format retrieval or validation

## Usage
