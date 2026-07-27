#nullable enable

using FluentAssertions;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class ExportRequestTests
{
    [Fact]
    public void Properties_ShouldAssignAndRetrieveCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var filters = new Dictionary<string, object> { { "key", "value" } };
        var fields = new List<string> { "field1", "field2" };

        // Act
        var request = new ExportRequest
        {
            TenantId = tenantId,
            ResourceType = "testType",
            Format = ExportFormat.Csv,
            Filters = filters,
            IncludeFields = fields
        };

        // Assert
        request.TenantId.Should().Be(tenantId);
        request.ResourceType.Should().Be("testType");
        request.Format.Should().Be(ExportFormat.Csv);
        request.Filters.Should().BeEquivalentTo(filters);
        request.IncludeFields.Should().BeEquivalentTo(fields);
    }

    [Fact]
    public void OptionalProperties_CanBeNull()
    {
        // Arrange
        var request = new ExportRequest();

        // Assert
        request.Filters.Should().BeNull();
        request.IncludeFields.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_ShouldBeExpected()
    {
        // Arrange
        var request = new ExportRequest();

        // Assert
        request.TenantId.Should().Be(Guid.Empty);
        request.ResourceType.Should().Be(string.Empty);
        request.Format.Should().Be(ExportFormat.Json);
    }

    [Fact]
    public void Collections_CanBeAssignedEmpty()
    {
        // Arrange
        var request = new ExportRequest
        {
            Filters = new Dictionary<string, object>(),
            IncludeFields = new List<string>()
        };

        // Assert
        request.Filters.Should().BeEmpty();
        request.IncludeFields.Should().BeEmpty();
    }

    [Fact]
    public void ResourceType_CanBeSetToEmptyString()
    {
        // Arrange
        var request = new ExportRequest { ResourceType = "" };

        // Assert
        request.ResourceType.Should().Be(string.Empty);
    }
}
