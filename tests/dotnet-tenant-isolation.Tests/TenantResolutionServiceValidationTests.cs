// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TenantIsolation.Configuration;
using TenantIsolation.Models;
using TenantIsolation.Services;
using TenantIsolation.Constants;
using Xunit;

namespace TenantIsolation.Tests;

public sealed class TenantResolutionServiceValidationTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Tenant TestTenant = new()
    {
        Id = TestTenantId,
        Name = "Test Tenant",
        Slug = "test-tenant",
        Status = TenantStatus.Active
    };

    private static TenantResolutionService CreateValidService()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var dynamicTenantStoreMock = new Mock<IDynamicTenantStore>();
        var loggerMock = new Mock<ILogger<TenantResolutionService>>();
        var optionsMock = new Mock<IOptions<TenantResolutionOptions>>();

        var options = new TenantResolutionOptions
        {
            ResolutionStrategies = new List<TenantResolutionStrategy> { TenantResolutionStrategy.Default }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var service = new TenantResolutionService(
            httpContextAccessorMock.Object,
            dynamicTenantStoreMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        return service;
    }

    [Fact]
    public void Validate_WithValidService_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateValidService();

        // Act
        var result = service.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        TenantResolutionService? service = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service!.Validate());
    }

    [Fact]
    public void IsValid_WithValidService_ReturnsTrue()
    {
        // Arrange
        var service = CreateValidService();

        // Act
        var result = service.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithNullService_ReturnsFalse()
    {
        // Arrange
        TenantResolutionService? service = null;

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithInvalidService_ReturnsFalse()
    {
        // Arrange
        var service = CreateValidService();

        // Override private fields to make service invalid
        // This simulates what the validation methods check for
        var httpContextAccessorField = typeof(TenantResolutionService).GetField(
            "_httpContextAccessor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpContextAccessorField?.SetValue(service, null);

        // Act
        var result = service.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnsureValid_WithValidService_DoesNotThrow()
    {
        // Arrange
        var service = CreateValidService();

        // Act & Assert
        service.EnsureValid(); // Should not throw
    }

    [Fact]
    public void EnsureValid_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        TenantResolutionService? service = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_WithInvalidService_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateValidService();

        // Make service invalid by nulling a dependency
        var httpContextAccessorField = typeof(TenantResolutionService).GetField(
            "_httpContextAccessor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpContextAccessorField?.SetValue(service, null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => service.EnsureValid());
        Assert.Contains("TenantResolutionService instance is invalid", exception.Message);
        Assert.Contains("IHttpContextAccessor dependency is null", exception.Message);
    }

    [Fact]
    public void Validate_WithMissingHttpContextAccessor_ReturnsProblem()
    {
        // Arrange
        var service = CreateValidService();

        // Make service invalid by nulling the dependency
        var httpContextAccessorField = typeof(TenantResolutionService).GetField(
            "_httpContextAccessor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpContextAccessorField?.SetValue(service, null);

        // Act
        var result = service.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("IHttpContextAccessor dependency is null.", result);
    }

    [Fact]
    public void Validate_WithMissingDynamicTenantStore_ReturnsProblem()
    {
        // Arrange
        var service = CreateValidService();

        // Make service invalid by nulling the dependency
        var dynamicTenantStoreField = typeof(TenantResolutionService).GetField(
            "_dynamicTenantStore",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dynamicTenantStoreField?.SetValue(service, null);

        // Act
        var result = service.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("IDynamicTenantStore dependency is null.", result);
    }

    [Fact]
    public void Validate_WithMissingLogger_ReturnsProblem()
    {
        // Arrange
        var service = CreateValidService();

        // Make service invalid by nulling the dependency
        var loggerField = typeof(TenantResolutionService).GetField(
            "_logger",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        loggerField?.SetValue(service, null);

        // Act
        var result = service.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("ILogger<TenantResolutionService> dependency is null.", result);
    }

    [Fact]
    public void Validate_WithMultipleMissingDependencies_ReturnsAllProblems()
    {
        // Arrange
        var service = CreateValidService();

        // Make service invalid by nulling multiple dependencies
        var httpContextAccessorField = typeof(TenantResolutionService).GetField(
            "_httpContextAccessor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        httpContextAccessorField?.SetValue(service, null);

        var dynamicTenantStoreField = typeof(TenantResolutionService).GetField(
            "_dynamicTenantStore",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dynamicTenantStoreField?.SetValue(service, null);

        // Act
        var result = service.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}