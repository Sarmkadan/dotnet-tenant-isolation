#nullable enable

using System;
using FluentAssertions;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class ExportRequestExtensionsTests
{
    [Fact]
    public void IsValid_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithEmptyResourceType_ReturnsFalse()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithWhitespaceResourceType_ReturnsFalse()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "   ",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyTenantId_ReturnsFalse()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.Empty,
            ResourceType = "users",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ExportRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.IsValid());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_WithInvalidResourceType_ReturnsFalse(string resourceType)
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = resourceType,
            Format = ExportFormat.Json
        };

        // Act
        var result = request.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFileName_WithValidRequest_ReturnsCorrectFormat()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.GetFileName();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Match("users_*_*.json");
        result.Should().Contain("users");
        result.Should().EndWith(".json");
    }

    [Fact]
    public void GetFileName_WithCsvFormat_ReturnsCsvExtension()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "products",
            Format = ExportFormat.Csv
        };

        // Act
        var result = request.GetFileName();

        // Assert
        result.Should().EndWith(".csv");
    }

    [Fact]
    public void GetFileName_WithXmlFormat_ReturnsXmlExtension()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "orders",
            Format = ExportFormat.Xml
        };

        // Act
        var result = request.GetFileName();

        // Assert
        result.Should().EndWith(".xml");
    }

    [Fact]
    public void GetFileName_WithUnknownFormat_ReturnsTxtExtension()
    {
        // Arrange - This tests the default case in the switch statement
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "logs",
            Format = (ExportFormat)999 // Unknown format
        };

        // Act
        var result = request.GetFileName();

        // Assert
        result.Should().EndWith(".txt");
    }

    [Fact]
    public void GetFileName_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ExportRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetFileName());
    }

    [Fact]
    public void GetContentType_WithJsonFormat_ReturnsApplicationJson()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            Format = ExportFormat.Json
        };

        // Act
        var result = request.GetContentType();

        // Assert
        result.Should().Be("application/json");
    }

    [Fact]
    public void GetContentType_WithCsvFormat_ReturnsTextCsv()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "products",
            Format = ExportFormat.Csv
        };

        // Act
        var result = request.GetContentType();

        // Assert
        result.Should().Be("text/csv");
    }

    [Fact]
    public void GetContentType_WithXmlFormat_ReturnsApplicationXml()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "orders",
            Format = ExportFormat.Xml
        };

        // Act
        var result = request.GetContentType();

        // Assert
        result.Should().Be("application/xml");
    }

    [Fact]
    public void GetContentType_WithUnknownFormat_ReturnsOctetStream()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "logs",
            Format = (ExportFormat)999 // Unknown format
        };

        // Act
        var result = request.GetContentType();

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void GetContentType_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ExportRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetContentType());
    }

    [Fact]
    public void GetExportOptions_WithValidRequest_ReturnsDictionaryWithAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var filters = new Dictionary<string, object> { { "status", "active" }, { "role", "admin" } };
        var request = new ExportRequest
        {
            TenantId = tenantId,
            ResourceType = "users",
            Format = ExportFormat.Json,
            Filters = filters,
            IncludeFields = new List<string> { "Id", "Name", "Email" }
        };

        // Act
        var result = request.GetExportOptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5); // ResourceType, Format, TenantId, Filters, IncludeFields
        result.Should().ContainKey("ResourceType").WhoseValue.Should().Be("users");
        result.Should().ContainKey("Format").WhoseValue.Should().Be(ExportFormat.Json);
        result.Should().ContainKey("TenantId").WhoseValue.Should().Be(tenantId);
        result.Should().ContainKey("Filters").WhoseValue.Should().BeSameAs(filters);
        result.Should().ContainKey("IncludeFields").WhoseValue.As<List<string>>().Should().BeEquivalentTo(new List<string> { "Id", "Name", "Email" });
    }

    [Fact]
    public void GetExportOptions_WithEmptyFiltersAndIncludeFields_ReturnsDictionaryWithoutOptionalKeys()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "products",
            Format = ExportFormat.Csv,
            Filters = new Dictionary<string, object>(),
            IncludeFields = new List<string>()
        };

        // Act
        var result = request.GetExportOptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // Only required properties
        result.Should().NotContainKey("Filters");
        result.Should().NotContainKey("IncludeFields");
    }

    [Fact]
    public void GetExportOptions_WithNullFilters_ReturnsDictionaryWithoutFilters()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "orders",
            Format = ExportFormat.Xml,
            Filters = null,
            IncludeFields = new List<string> { "Id", "Amount" }
        };

        // Act
        var result = request.GetExportOptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // ResourceType, Format, TenantId, IncludeFields
        result.Should().NotContainKey("Filters");
        result.Should().ContainKey("IncludeFields");
    }

    [Fact]
    public void GetExportOptions_WithNullIncludeFields_ReturnsDictionaryWithoutIncludeFields()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "logs",
            Format = ExportFormat.Json,
            Filters = new Dictionary<string, object> { { "level", "error" } },
            IncludeFields = null
        };

        // Act
        var result = request.GetExportOptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // ResourceType, Format, TenantId, Filters
        result.Should().NotContainKey("IncludeFields");
    }

    [Fact]
    public void GetExportOptions_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ExportRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetExportOptions());
    }

    [Fact]
    public void ShouldIncludeField_WithNullIncludeFields_ReturnsTrue()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            IncludeFields = null
        };

        // Act
        var result = request.ShouldIncludeField("Name");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldIncludeField_WithEmptyIncludeFields_ReturnsTrue()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            IncludeFields = new List<string>()
        };

        // Act
        var result = request.ShouldIncludeField("Name");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldIncludeField_WithMatchingField_ReturnsTrue()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            IncludeFields = new List<string> { "Id", "Name", "Email" }
        };

        // Act
        var result = request.ShouldIncludeField("Name");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldIncludeField_WithNonMatchingField_ReturnsFalse()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            IncludeFields = new List<string> { "Id", "Name", "Email" }
        };

        // Act
        var result = request.ShouldIncludeField("Age");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldIncludeField_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users"
        };
        string? nullFieldName = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request.ShouldIncludeField(nullFieldName!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  \t  ")]
    public void ShouldIncludeField_WithWhitespaceFieldName_ThrowsArgumentException(string whitespaceFieldName)
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => request.ShouldIncludeField(whitespaceFieldName));
    }

    [Fact]
    public void ShouldIncludeField_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        ExportRequest? nullRequest = null;
        var fieldName = "Name";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.ShouldIncludeField(fieldName));
    }

    [Fact]
    public void ShouldIncludeField_WithCaseSensitiveMatching_ReturnsFalseForDifferentCase()
    {
        // Arrange
        var request = new ExportRequest
        {
            TenantId = Guid.NewGuid(),
            ResourceType = "users",
            IncludeFields = new List<string> { "Name", "Email" }
        };

        // Act
        var result = request.ShouldIncludeField("name");

        // Assert
        result.Should().BeFalse();
    }
}