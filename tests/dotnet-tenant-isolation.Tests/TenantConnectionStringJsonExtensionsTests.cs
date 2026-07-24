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
        // but it is assumed to have a parameterless constructor and public settable
        // properties for the purpose of these tests. Adjust the initializer as needed
        // to match the actual model definition.
        return new TenantConnectionString
        {
            // Example properties – replace with real ones if they differ
            // TenantId = Guid.NewGuid(),
            // ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
        };
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
    public void ToJson_WithIndentation_ShouldSetWriteIndented()
    {
        var value = CreateSample();

        string json = value.ToJson(indented: true);

        Assert.Contains("\n", json); // indented JSON contains line breaks
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
    public void FromJson_NullOrEmpty_ShouldThrowArgumentException()
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
    public void TryFromJson_NullArgument_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TenantConnectionStringJsonExtensions.TryFromJson(null!, out _));
    }
}
