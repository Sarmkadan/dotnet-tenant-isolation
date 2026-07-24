// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public sealed class TenantConnectionStringJsonExtensionsTests
{
    private static TenantConnectionString CreateSample()
    {
        // The concrete shape of TenantConnectionString is not known here,
        // but it is assumed to have a public parameterless constructor.
        // If the type defines required properties, they can be left at their defaults
        // for the purpose of serialization tests.
        return new TenantConnectionString();
    }

    [Fact]
    public void ToJson_ShouldSerializeObject()
    {
        var value = CreateSample();

        string json = value.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should be deserializable back to the same type
        var deserialized = JsonSerializer.Deserialize<TenantConnectionString>(json);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void ToJson_WithIndentation_ShouldDifferFromNonIndented()
    {
        var value = CreateSample();

        string jsonNonIndented = value.ToJson(indented: false);
        string jsonIndented = value.ToJson(indented: true);

        // When indentation is requested the output should differ (e.g., contain line breaks or spaces)
        Assert.NotEqual(jsonNonIndented, jsonIndented);
    }

    [Fact]
    public void ToJson_NullArgument_ShouldThrowArgumentNullException()
    {
        TenantConnectionString? nullValue = null;
        Assert.Throws<ArgumentNullException>(() => nullValue!.ToJson());
    }

    [Fact]
    public void FromJson_ValidJson_ShouldReturnObject()
    {
        var original = CreateSample();
        string json = original.ToJson();

        var result = TenantConnectionStringJsonExtensions.FromJson(json);

        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_NullOrEmptyOrWhiteSpace_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TenantConnectionStringJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => TenantConnectionStringJsonExtensions.FromJson(string.Empty));
        Assert.Throws<ArgumentException>(() => TenantConnectionStringJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void TryFromJson_ValidJson_ShouldReturnTrue()
    {
        var original = CreateSample();
        string json = original.ToJson();

        bool success = TenantConnectionStringJsonExtensions.TryFromJson(json, out var result);

        Assert.True(success);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ShouldReturnFalse()
    {
        const string invalidJson = "{ this is not valid json }";

        bool success = TenantConnectionStringJsonExtensions.TryFromJson(invalidJson, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WhiteSpace_ShouldReturnFalse()
    {
        bool success = TenantConnectionStringJsonExtensions.TryFromJson("   ", out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_NullArgument_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TenantConnectionStringJsonExtensions.TryFromJson(null!, out _));
    }
}
