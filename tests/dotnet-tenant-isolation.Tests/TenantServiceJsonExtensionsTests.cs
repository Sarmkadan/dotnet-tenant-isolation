// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public sealed class TenantServiceJsonExtensionsTests
{
    private static TenantService CreateMockTenantService()
    {
        // Since TenantService has required constructor dependencies, we use a minimal mock approach
        return new TenantService(
            tenantRepository: null!,  // Mock - not used in serialization
            dynamicTenantStore: null!, // Mock - not used in serialization
            logger: null!            // Mock - not used in serialization
        );
    }

    [Fact]
    public void ToJson_ShouldSerializeObject()
    {
        var tenantService = CreateMockTenantService();

        string json = tenantService.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_WithIndentation_ShouldDifferFromNonIndented()
    {
        var tenantService = CreateMockTenantService();

        string jsonNonIndented = tenantService.ToJson(indented: false);
        string jsonIndented = tenantService.ToJson(indented: true);

        Assert.NotEqual(jsonNonIndented, jsonIndented);
    }

    [Fact]
    public void ToJson_NullArgument_ShouldThrowArgumentNullException()
    {
        TenantService? nullTenantService = null;
        Assert.Throws<ArgumentNullException>(() => nullTenantService!.ToJson());
    }

    [Fact]
    public void ToJson_IndentedOutput_ShouldContainNewlines()
    {
        var tenantService = CreateMockTenantService();
        string json = tenantService.ToJson(indented: true);

        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var tenantService = CreateMockTenantService();
        string json = tenantService.ToJson();

        var parsed = JsonSerializer.Deserialize<object>(json);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ShouldProduceCompactJson()
    {
        var tenantService = CreateMockTenantService();
        string json = tenantService.ToJson(indented: false);

        Assert.DoesNotContain("\n", json);
    }

    [Fact]
    public void FromJson_NullOrEmptyOrWhiteSpace_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TenantServiceJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => TenantServiceJsonExtensions.FromJson(string.Empty));
        Assert.Throws<ArgumentException>(() => TenantServiceJsonExtensions.FromJson(" "));
    }

    [Fact]
    public void TryFromJson_NullArgument_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TenantServiceJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_WhiteSpace_ShouldReturnFalseAndNull()
    {
        bool success = TenantServiceJsonExtensions.TryFromJson(" ", out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_EmptyString_ShouldReturnFalseAndNull()
    {
        bool success = TenantServiceJsonExtensions.TryFromJson(string.Empty, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void ToJson_ProducesNonEmptyOutput()
    {
        var tenantService = CreateMockTenantService();
        string json = tenantService.ToJson();

        Assert.NotEmpty(json);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ShouldProduceFormattedJson()
    {
        var tenantService = CreateMockTenantService();
        string json = tenantService.ToJson(indented: true);

        var lines = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length > 1);
    }
}