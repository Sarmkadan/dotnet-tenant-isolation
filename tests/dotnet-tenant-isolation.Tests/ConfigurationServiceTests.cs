#nullable enable

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;
using Xunit;

namespace TenantIsolation.Tests;

public class ConfigurationServiceTests : IAsyncLifetime
{
    private readonly TenantDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly ConfigurationService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"ConfigurationServiceTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TenantDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<ConfigurationService>>();

        _sut = new ConfigurationService(_dbContext, _cache, _mockLogger.Object);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _cache.Dispose();
        await _dbContext.DisposeAsync();
    }

    #region SetConfigurationAsync Tests

    [Fact]
    public async Task SetConfigurationAsync_WithNewKey_CreatesConfiguration()
    {
        // Arrange
        const string key = "api-key";
        const string value = "secret123";

        // Act
        var result = await _sut.SetConfigurationAsync(_tenantId, key, value);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(_tenantId);
        result.Key.Should().Be(key);
        result.Value.Should().Be(value);
        result.ValueType.Should().Be("string");
        result.IsEncrypted.Should().BeFalse();

        var stored = await _dbContext.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == _tenantId && c.Key == key);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task SetConfigurationAsync_WithExistingKey_UpdatesConfiguration()
    {
        // Arrange
        const string key = "api-key";
        const string oldValue = "old-secret";
        const string newValue = "new-secret";

        await _sut.SetConfigurationAsync(_tenantId, key, oldValue);

        // Act
        var result = await _sut.SetConfigurationAsync(_tenantId, key, newValue);

        // Assert
        result.Value.Should().Be(newValue);

        var count = await _dbContext.TenantConfigurations
            .Where(c => c.TenantId == _tenantId && c.Key == key)
            .CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SetConfigurationAsync_WithEncryption_SetsEncryptionFlag()
    {
        // Arrange
        const string key = "password";
        const string value = "secret";

        // Act
        var result = await _sut.SetConfigurationAsync(_tenantId, key, value, "string", true);

        // Assert
        result.IsEncrypted.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetConfigurationAsync_WithNullOrWhitespaceKey_ThrowsException(string? key)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantConfigurationException>(
            () => _sut.SetConfigurationAsync(_tenantId, key!, "value"));

        ex.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task SetConfigurationAsync_InvalidatesCacheAfterSet()
    {
        // Arrange
        const string key = "cache-test";
        const string value = "test-value";

        // Pre-populate cache with old value
        var cacheKey = $"config_{_tenantId}_{key}";
        _cache.Set(cacheKey, new TenantConfiguration { Value = "old" });

        // Act
        await _sut.SetConfigurationAsync(_tenantId, key, value);

        // Assert
        var cached = _cache.Get<TenantConfiguration>(cacheKey);
        cached.Should().BeNull();
    }

    #endregion

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_WithExistingConfiguration_ReturnsConfiguration()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        await _sut.SetConfigurationAsync(_tenantId, key, value);

        // Act
        var result = await _sut.GetConfigurationAsync(_tenantId, key);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.Value.Should().Be(value);
    }

    [Fact]
    public async Task GetConfigurationAsync_CachesResultAfterFirstCall()
    {
        // Arrange
        const string key = "cached-key";
        const string value = "cached-value";

        var config = new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Key = key,
            Value = value,
            ValueType = "string"
        };
        _dbContext.TenantConfigurations.Add(config);
        await _dbContext.SaveChangesAsync();

        // Act
        var firstCall = await _sut.GetConfigurationAsync(_tenantId, key);

        // Verify it's in cache
        var cacheKey = $"config_{_tenantId}_{key}";
        var cached = _cache.Get<TenantConfiguration>(cacheKey);

        // Assert
        cached.Should().NotBeNull();
        cached!.Value.Should().Be(value);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNonExistentKey_ReturnsNull()
    {
        // Act
        var result = await _sut.GetConfigurationAsync(_tenantId, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetConfigurationAsync<T> Tests

    [Fact]
    public async Task GetConfigurationAsync_Generic_ConvertsStringToInt()
    {
        // Arrange
        const string key = "max-users";
        const string value = "100";

        await _sut.SetConfigurationAsync(_tenantId, key, value, "int");

        // Act
        var result = await _sut.GetConfigurationAsync<int>(_tenantId, key);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public async Task GetConfigurationAsync_Generic_ConvertsToBool()
    {
        // Arrange
        const string key = "feature-flag";
        const string value = "true";

        await _sut.SetConfigurationAsync(_tenantId, key, value, "bool");

        // Act
        var result = await _sut.GetConfigurationAsync<bool>(_tenantId, key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigurationAsync_Generic_ReturnsDefaultWhenNotFound()
    {
        // Act
        var result = await _sut.GetConfigurationAsync<int>(_tenantId, "nonexistent", 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetConfigurationAsync_Generic_WithConversionError_ThrowsException()
    {
        // Arrange
        const string key = "bad-int";
        const string value = "not-a-number";

        await _sut.SetConfigurationAsync(_tenantId, key, value, "int");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantConfigurationException>(
            () => _sut.GetConfigurationAsync<int>(_tenantId, key));

        ex.Message.Should().Contain("Failed to convert");
    }

    #endregion

    #region DeleteConfigurationAsync Tests

    [Fact]
    public async Task DeleteConfigurationAsync_WithExistingKey_DeletesConfiguration()
    {
        // Arrange
        const string key = "delete-me";
        await _sut.SetConfigurationAsync(_tenantId, key, "value");

        // Act
        var result = await _sut.DeleteConfigurationAsync(_tenantId, key);

        // Assert
        result.Should().BeTrue();

        var config = await _dbContext.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == _tenantId && c.Key == key);
        config.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfigurationAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteConfigurationAsync(_tenantId, "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteConfigurationAsync_InvalidatesCacheAfterDelete()
    {
        // Arrange
        const string key = "cache-delete";
        await _sut.SetConfigurationAsync(_tenantId, key, "value");

        var cacheKey = $"config_{_tenantId}_{key}";
        _cache.Set(cacheKey, new TenantConfiguration { Value = "cached" });

        // Act
        await _sut.DeleteConfigurationAsync(_tenantId, key);

        // Assert
        var cached = _cache.Get<TenantConfiguration>(cacheKey);
        cached.Should().BeNull();
    }

    #endregion

    #region GetAllConfigurationsAsync Tests

    [Fact]
    public async Task GetAllConfigurationsAsync_ReturnsAllConfigurationsForTenant()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "key1", "value1");
        await _sut.SetConfigurationAsync(_tenantId, "key2", "value2");
        await _sut.SetConfigurationAsync(_tenantId, "key3", "value3");

        var otherTenant = Guid.NewGuid();
        await _sut.SetConfigurationAsync(otherTenant, "other-key", "other-value");

        // Act
        var result = await _sut.GetAllConfigurationsAsync(_tenantId);

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().Contain(new[] { "key1", "key2", "key3" });
        result.Should().NotContainKey("other-key");
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_CachesResult()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "key1", "value1");

        // Act
        var firstCall = await _sut.GetAllConfigurationsAsync(_tenantId);

        var cacheKey = $"config_all_{_tenantId}";
        var cached = _cache.Get<Dictionary<string, string>>(cacheKey);

        // Assert
        cached.Should().NotBeNull();
        cached.Should().HaveCount(1);
    }

    #endregion

    #region HasConfigurationAsync Tests

    [Fact]
    public async Task HasConfigurationAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "exists";
        await _sut.SetConfigurationAsync(_tenantId, key, "value");

        // Act
        var result = await _sut.HasConfigurationAsync(_tenantId, key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasConfigurationAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _sut.HasConfigurationAsync(_tenantId, "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetConfigurationKeysAsync Tests

    [Fact]
    public async Task GetConfigurationKeysAsync_WithWildcardPattern_ReturnsAllKeys()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "feature-enabled", "true");
        await _sut.SetConfigurationAsync(_tenantId, "feature-timeout", "30");
        await _sut.SetConfigurationAsync(_tenantId, "api-key", "secret");

        // Act
        var result = await _sut.GetConfigurationKeysAsync(_tenantId, "*");

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetConfigurationKeysAsync_WithPattern_ReturnsMatchingKeys()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "feature-enabled", "true");
        await _sut.SetConfigurationAsync(_tenantId, "feature-timeout", "30");
        await _sut.SetConfigurationAsync(_tenantId, "api-key", "secret");

        // Act
        var result = await _sut.GetConfigurationKeysAsync(_tenantId, "feature-*");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "feature-enabled", "feature-timeout" });
    }

    #endregion

    #region SetConfigurationBatchAsync Tests

    [Fact]
    public async Task SetConfigurationBatchAsync_CreatesMultipleConfigurations()
    {
        // Arrange
        var batch = new Dictionary<string, (string value, string type, bool encrypted)>
        {
            { "key1", ("value1", "string", false) },
            { "key2", ("value2", "string", false) },
            { "key3", ("value3", "int", false) }
        };

        // Act
        var count = await _sut.SetConfigurationBatchAsync(_tenantId, batch);

        // Assert
        count.Should().Be(3);

        var configs = await _dbContext.TenantConfigurations
            .Where(c => c.TenantId == _tenantId)
            .ToListAsync();
        configs.Should().HaveCount(3);
    }

    #endregion

    #region ExportConfigurationAsync Tests

    [Fact]
    public async Task ExportConfigurationAsync_ReturnsJsonString()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "key1", "value1");
        await _sut.SetConfigurationAsync(_tenantId, "key2", "value2");

        // Act
        var result = await _sut.ExportConfigurationAsync(_tenantId);

        // Assert
        result.Should().Contain("key1");
        result.Should().Contain("value1");
        result.Should().Contain("key2");
        result.Should().Contain("value2");
    }

    #endregion

    #region ImportConfigurationAsync Tests

    [Fact]
    public async Task ImportConfigurationAsync_ImportsConfigurationsFromJson()
    {
        // Arrange
        var jsonConfig = @"{ ""key1"": ""value1"", ""key2"": ""value2"" }";

        // Act
        var count = await _sut.ImportConfigurationAsync(_tenantId, jsonConfig);

        // Assert
        count.Should().Be(2);

        var config1 = await _sut.GetConfigurationAsync(_tenantId, "key1");
        config1!.Value.Should().Be("value1");

        var config2 = await _sut.GetConfigurationAsync(_tenantId, "key2");
        config2!.Value.Should().Be("value2");
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithInvalidJson_ThrowsException()
    {
        // Arrange
        const string invalidJson = "{ invalid json }";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantConfigurationException>(
            () => _sut.ImportConfigurationAsync(_tenantId, invalidJson));

        ex.Message.Should().Contain("Failed to parse JSON");
    }

    [Fact]
    public async Task ImportConfigurationAsync_WithEmptyJson_ThrowsException()
    {
        // Arrange
        const string emptyJson = "null";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantConfigurationException>(
            () => _sut.ImportConfigurationAsync(_tenantId, emptyJson));

        ex.Message.Should().Contain("Invalid JSON format");
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCounts()
    {
        // Arrange
        await _sut.SetConfigurationAsync(_tenantId, "key1", "value1", "string", false);
        await _sut.SetConfigurationAsync(_tenantId, "key2", "value2", "string", true);
        await _sut.SetConfigurationAsync(_tenantId, "key3", "value3", "int", true);

        // Act
        var result = await _sut.GetStatisticsAsync(_tenantId);

        // Assert
        result.Should().NotBeNull();
        var stats = (dynamic)result;
        Assert.Equal(3, (int)stats.TotalConfigurations);
        Assert.Equal(2, (int)stats.EncryptedConfigurations);
    }

    #endregion

    #region ValidateRequiredConfigurationsAsync Tests

    [Fact]
    public async Task ValidateRequiredConfigurationsAsync_WithAllRequiredPresent_ReturnsTrue()
    {
        // Arrange
        var required = new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Key = "required-key",
            Value = "required-value",
            IsRequired = true
        };
        _dbContext.TenantConfigurations.Add(required);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.ValidateRequiredConfigurationsAsync(_tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRequiredConfigurationsAsync_WithMissingRequired_ReturnsFalse()
    {
        // Arrange
        var missingRequired = new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Key = "required-key",
            Value = "",
            IsRequired = true
        };
        _dbContext.TenantConfigurations.Add(missingRequired);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.ValidateRequiredConfigurationsAsync(_tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRequiredConfigurationsAsync_WithNoRequiredConfigs_ReturnsTrue()
    {
        // Act
        var result = await _sut.ValidateRequiredConfigurationsAsync(_tenantId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
