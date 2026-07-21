#nullable enable

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Unit tests for <see cref="TenantFeatureService"/> class.
/// </summary>
public class TenantFeatureServiceTests
{
    private readonly TenantDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<TenantFeatureService>> _mockLogger;
    private readonly TenantFeatureService _sut;

    public TenantFeatureServiceTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"TenantFeatureServiceTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<TenantFeatureService>>();

        _sut = new TenantFeatureService(_dbContext, _cache, _mockLogger.Object);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WithUnknownFeature_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.IsFeatureEnabledAsync(tenantId, "unknown-feature");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNewFeature_EnablesAndReturnsFeature()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string featureKey = "test-feature";

        // Act
        var result = await _sut.EnableFeatureAsync(tenantId, featureKey);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenantId);
        result.FeatureKey.Should().Be(featureKey);
        result.IsEnabled.Should().BeTrue();
        result.RolloutPercentage.Should().Be(100);

        var isEnabled = await _sut.IsFeatureEnabledAsync(tenantId, featureKey);
        isEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisableFeatureAsync_WithExistingEnabledFeature_DisablesAndReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string featureKey = "test-feature";
        await _sut.EnableFeatureAsync(tenantId, featureKey);

        // Act
        var result = await _sut.DisableFeatureAsync(tenantId, featureKey);

        // Assert
        result.Should().BeTrue();
        var isEnabled = await _sut.IsFeatureEnabledAsync(tenantId, featureKey);
        isEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task DisableFeatureAsync_WithNonExistentFeature_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.DisableFeatureAsync(tenantId, "non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnableFeatureAsync_WithExistingDisabledFeature_EnablesFeature()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string featureKey = "test-feature";
        await _sut.EnableFeatureAsync(tenantId, featureKey);
        await _sut.DisableFeatureAsync(tenantId, featureKey);

        // Act
        var result = await _sut.EnableFeatureAsync(tenantId, featureKey);

        // Assert
        result.IsEnabled.Should().BeTrue();
        var isEnabled = await _sut.IsFeatureEnabledAsync(tenantId, featureKey);
        isEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WithUnknownTenant_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _sut.IsFeatureEnabledAsync(tenantId, "any-feature");

        // Assert
        result.Should().BeFalse();
    }
}
