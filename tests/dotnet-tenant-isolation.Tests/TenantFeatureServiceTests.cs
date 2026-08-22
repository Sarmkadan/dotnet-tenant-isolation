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

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(IsFeatureEnabledAsync_WithUnknownFeature_ReturnsFalse),
                                          tenantId,
                                          "unknown-feature");

        // Act
        var result = await _sut.IsFeatureEnabledAsync(tenantId, "unknown-feature");

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(IsFeatureEnabledAsync_WithUnknownFeature_ReturnsFalse),
                                          result);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnableFeatureAsync_WithNewFeature_EnablesAndReturnsFeature()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string featureKey = "test-feature";

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(EnableFeatureAsync_WithNewFeature_EnablesAndReturnsFeature),
                                          tenantId,
                                          featureKey);

        // Act
        var result = await _sut.EnableFeatureAsync(tenantId, featureKey);

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(EnableFeatureAsync_WithNewFeature_EnablesAndReturnsFeature),
                                          result);

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

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(DisableFeatureAsync_WithExistingEnabledFeature_DisablesAndReturnsTrue),
                                          tenantId,
                                          featureKey);

        // Act
        var result = await _sut.DisableFeatureAsync(tenantId, featureKey);

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(DisableFeatureAsync_WithExistingEnabledFeature_DisablesAndReturnsTrue),
                                          result);

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

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(DisableFeatureAsync_WithNonExistentFeature_ReturnsFalse),
                                          tenantId,
                                          "non-existent");

        // Act
        var result = await _sut.DisableFeatureAsync(tenantId, "non-existent");

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(DisableFeatureAsync_WithNonExistentFeature_ReturnsFalse),
                                          result);

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

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(EnableFeatureAsync_WithExistingDisabledFeature_EnablesFeature),
                                          tenantId,
                                          featureKey);

        // Act
        var result = await _sut.EnableFeatureAsync(tenantId, featureKey);

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(EnableFeatureAsync_WithExistingDisabledFeature_EnablesFeature),
                                          result);

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

        // Log start
        _mockLogger.Object.LogInformation("Starting test {TestName} with TenantId={TenantId}, FeatureKey={FeatureKey}",
                                          nameof(IsFeatureEnabledAsync_WithUnknownTenant_ReturnsFalse),
                                          tenantId,
                                          "any-feature");

        // Act
        var result = await _sut.IsFeatureEnabledAsync(tenantId, "any-feature");

        // Log end
        _mockLogger.Object.LogInformation("Finished test {TestName} with result={Result}",
                                          nameof(IsFeatureEnabledAsync_WithUnknownTenant_ReturnsFalse),
                                          result);

        // Assert
        result.Should().BeFalse();
    }
}
