using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Services;
using System.Text;
using Xunit;

/// <summary>
/// Tests for the ExportService class.
/// </summary>
public class ExportServiceTests
{
    private readonly Mock<ILogger<ExportService>> _loggerMock;
    private readonly ExportService _exportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportServiceTests"/> class.
    /// </summary>
    public ExportServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExportService>>();
        _exportService = new ExportService(_loggerMock.Object);
    }

    /// <summary>
    /// Tests that ExportAsync throws an ArgumentNullException when the request is null.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _exportService.ExportAsync(null!, new List<object>());

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ExportAsync returns a valid JSON response when the format is JSON.
    /// </summary>
    [Fact]
    public async Task ExportAsync_JsonFormat_ReturnsValidJson()
    {
        // Arrange
        var request = new ExportRequest { TenantId = Guid.NewGuid(), ResourceType = "Test", Format = ExportFormat.Json };
        var data = new List<object> { new { Name = "TestItem" } };

        // Act
        var result = await _exportService.ExportAsync(request, data);

        // Assert
        result.Format.Should().Be(ExportFormat.Json);
        result.ContentType.Should().Be("application/json");
        Encoding.UTF8.GetString(result.Content).Should().Contain("Name");
        Encoding.UTF8.GetString(result.Content).Should().Contain("TestItem");
    }

    /// <summary>
    /// Tests that ExportAsync returns a valid CSV response when the format is CSV.
    /// </summary>
    [Fact]
    public async Task ExportAsync_CsvFormat_ReturnsValidCsv()
    {
        // Arrange
        var request = new ExportRequest { TenantId = Guid.NewGuid(), ResourceType = "Test", Format = ExportFormat.Csv };
        var data = new List<object> { new { Name = "TestItem", Value = 1 } };

        // Act
        var result = await _exportService.ExportAsync(request, data);

        // Assert
        result.Format.Should().Be(ExportFormat.Csv);
        result.ContentType.Should().Be("text/csv");
        var csvContent = Encoding.UTF8.GetString(result.Content);
        csvContent.Should().Contain("Name,Value");
        csvContent.Should().Contain("TestItem,1");
    }

    /// <summary>
    /// Tests that ExportAsync returns a valid XML response when the format is XML.
    /// </summary>
    [Fact]
    public async Task ExportAsync_XmlFormat_ReturnsValidXml()
    {
        // Arrange
        var request = new ExportRequest { TenantId = Guid.NewGuid(), ResourceType = "Test", Format = ExportFormat.Xml };
        var data = new List<object> { new { Name = "TestItem" } };

        // Act
        var result = await _exportService.ExportAsync(request, data);

        // Assert
        result.Format.Should().Be(ExportFormat.Xml);
        result.ContentType.Should().Be("application/xml");
        Encoding.UTF8.GetString(result.Content).Should().Contain("<Test>");
        Encoding.UTF8.GetString(result.Content).Should().Contain("<Name>TestItem</Name>");
    }

    /// <summary>
    /// Tests that GetSupportedFormats returns all supported formats.
    /// </summary>
    [Fact]
    public void GetSupportedFormats_ReturnsAllFormats()
    {
        // Act
        var formats = _exportService.GetSupportedFormats("Any");

        // Assert
        formats.Should().HaveCount(3);
        formats.Should().Contain(new[] { ExportFormat.Json, ExportFormat.Csv, ExportFormat.Xml });
    }
}
