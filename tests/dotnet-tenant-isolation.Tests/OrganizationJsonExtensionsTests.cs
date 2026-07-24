using System;
using System.Text.Json;
using TenantIsolation.Models;
using Xunit;

namespace dotnet_tenant_isolation.Tests.Models;

public class OrganizationJsonExtensionsTests
{
    [Fact]
    public void ToJson_ValidObject_ReturnsJsonString()
    {
        // Arrange
        var org = new Organization();

        // Act
        var json = org.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_Indented_ReturnsFormattedJson()
    {
        // Arrange
        var org = new Organization();

        // Act
        var json = org.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void ToJson_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        Organization? org = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => org.ToJson());
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsOrganization()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = OrganizationJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OrganizationJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "invalid";

        // Act & Assert
        Assert.Throws<JsonException>(() => OrganizationJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndObject()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = OrganizationJsonExtensions.TryFromJson(json, out var org);

        // Assert
        Assert.True(result);
        Assert.NotNull(org);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "invalid";

        // Act
        var result = OrganizationJsonExtensions.TryFromJson(json, out var org);

        // Assert
        Assert.False(result);
        Assert.Null(org);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OrganizationJsonExtensions.TryFromJson(json, out _));
    }
}
