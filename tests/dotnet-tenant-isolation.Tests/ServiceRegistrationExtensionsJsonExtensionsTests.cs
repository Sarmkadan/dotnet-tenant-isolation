// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using Xunit;
using TenantIsolation.Configuration;

namespace TenantIsolation.Tests;

public class ServiceRegistrationExtensionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_NullArgument_ThrowsArgumentNullException()
    {
        TenantIsolationOptions? nullOptions = null;
        Assert.Throws<ArgumentNullException>(() => nullOptions!.ToJson());
    }

    [Fact]
    public void ToJson_HappyPath_ReturnsValidJson()
    {
        var options = new TenantIsolationOptions(); // default instance
        string json = options.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesFormattedJson()
    {
        var options = new TenantIsolationOptions();
        string jsonIndented = options.ToJson(indented: true);
        string jsonCompact = options.ToJson(indented: false);

        // Indented JSON should contain line breaks and indentation
        Assert.Contains("\n", jsonIndented);
        Assert.Contains("  ", jsonIndented);

        // Compact JSON should not contain line breaks
        Assert.DoesNotContain("\n", jsonCompact);
        Assert.NotEqual(jsonIndented, jsonCompact);
    }

    [Fact]
    public void FromJson_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationExtensionsJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsObject()
    {
        var options = new TenantIsolationOptions();
        string json = options.ToJson();

        var deserialized = ServiceRegistrationExtensionsJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        // Since we used a default instance with no custom properties, a simple type check suffices
        Assert.IsType<TenantIsolationOptions>(deserialized);
    }

    [Fact]
    public void TryFromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ServiceRegistrationExtensionsJsonExtensions.TryFromJson(null!, out _));
        Assert.Throws<ArgumentException>(() => ServiceRegistrationExtensionsJsonExtensions.TryFromJson(string.Empty, out _));
        Assert.Throws<ArgumentException>(() => ServiceRegistrationExtensionsJsonExtensions.TryFromJson("   ", out _));
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        const string invalidJson = "{ this is not valid json }";

        bool success = ServiceRegistrationExtensionsJsonExtensions.TryFromJson(invalidJson, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndObject()
    {
        var options = new TenantIsolationOptions();
        string json = options.ToJson();

        bool success = ServiceRegistrationExtensionsJsonExtensions.TryFromJson(json, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.IsType<TenantIsolationOptions>(result);
    }
}
