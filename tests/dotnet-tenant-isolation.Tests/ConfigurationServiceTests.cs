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

/// <summary>
/// Test suite for <see cref="ConfigurationService"/> functionality.
/// Tests cover configuration management operations including CRUD, caching, batch operations, and validation.
/// </summary>

public class ConfigurationServiceTests : IAsyncLifetime
{
	/// <summary>
	/// Database context for testing tenant configuration operations.
	/// </summary>
    private readonly TenantDbContext _dbContext;
	/// <summary>
	/// In-memory cache for testing caching behavior.
	/// </summary>
    private readonly IMemoryCache _cache;
	/// <summary>
	/// Mock logger for verifying logging behavior.
	/// </summary>
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
	/// <summary>
	/// System under test - the ConfigurationService instance being tested.
	/// </summary>
    private readonly ConfigurationService _sut;
	/// <summary>
	/// Test tenant identifier used across all test cases.
	/// </summary>
    private readonly Guid _tenantId = Guid.NewGuid();

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigurationServiceTests"/> class.
	/// Sets up in-memory database, cache, mock logger, and creates the ConfigurationService instance.
	/// </summary>
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

	/// <summary>
	/// Ensures the test database is created and ready for use.
	/// Called automatically by xUnit before each test.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

	/// <summary>
	/// Cleans up test resources including cache and database context.
	/// Called automatically by xUnit after each test.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        _cache.Dispose();
        await _dbContext.DisposeAsync();
    }

    #region SetConfigurationAsync Tests

    [Fact]
	/// <summary>
	/// Tests that SetConfigurationAsync creates a new configuration entry when the key doesn't exist.
	/// Verifies the returned configuration has correct properties and persists to database.
	/// </summary>
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
	/// <summary>
	/// Tests that SetConfigurationAsync updates an existing configuration entry.
	/// Verifies the value is updated and only one configuration entry exists.
	/// </summary>
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
	/// <summary>
	/// Tests that SetConfigurationAsync properly sets the IsEncrypted flag when encryption is requested.
	/// </summary>
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
	/// <summary>
	/// Tests that SetConfigurationAsync throws TenantConfigurationException when key is null, empty, or whitespace.
	/// </summary>
	/// <param name="key">The invalid key value to test.</param>
    public async Task SetConfigurationAsync_WithNullOrWhitespaceKey_ThrowsException(string? key)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<TenantConfigurationException>(
            () => _sut.SetConfigurationAsync(_tenantId, key!, "value"));

        ex.Message.Should().Contain("empty");
    }

    [Fact]
	/// <summary>
	/// Tests that SetConfigurationAsync invalidates the cache entry for the updated configuration.
	/// Verifies cache is cleared after setting a new value.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync returns the configuration when it exists.
	/// Verifies all properties of the returned configuration are correct.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync caches the result after the first call.
	/// Verifies the configuration is stored in cache and can be retrieved.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync returns null when the configuration key doesn't exist.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync&lt;T&gt; converts string configuration values to int.
	/// Verifies type conversion works correctly for integer values.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync&lt;T&gt; converts string configuration values to bool.
	/// Verifies type conversion works correctly for boolean values.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationAsync&lt;T&gt; returns the default value when configuration doesn't exist.
	/// </summary>
	/// <returns>The default value for the type.</returns>
    public async Task GetConfigurationAsync_Generic_ReturnsDefaultWhenNotFound()
    {
        // Act
        var result = await _sut.GetConfigurationAsync<int>(_tenantId, "nonexistent", 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
	/// <summary>
	/// Tests that GetConfigurationAsync&lt;T&gt; throws TenantConfigurationException when value conversion fails.
	/// Verifies proper error handling for invalid type conversions.
	/// </summary>
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
	/// <summary>
	/// Tests that DeleteConfigurationAsync removes the configuration entry when it exists.
	/// Verifies the configuration is deleted from database and returns true.
	/// </summary>
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
	/// <summary>
	/// Tests that DeleteConfigurationAsync returns false when trying to delete a non-existent configuration.
	/// </summary>
    public async Task DeleteConfigurationAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteConfigurationAsync(_tenantId, "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
	/// <summary>
	/// Tests that DeleteConfigurationAsync invalidates the cache entry after deletion.
	/// Verifies cache is cleared after deleting a configuration.
	/// </summary>
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
	/// <summary>
	/// Tests that GetAllConfigurationsAsync returns all configurations for a specific tenant.
	/// Verifies only configurations for the specified tenant are returned.
	/// </summary>
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
	/// <summary>
	/// Tests that GetAllConfigurationsAsync caches the result after the first call.
	/// Verifies the dictionary of configurations is stored in cache.
	/// </summary>
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
	/// <summary>
	/// Tests that HasConfigurationAsync returns true when the configuration key exists.
	/// </summary>
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
	/// <summary>
	/// Tests that HasConfigurationAsync returns false when the configuration key doesn't exist.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationKeysAsync returns all keys when using wildcard pattern.
	/// </summary>
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
	/// <summary>
	/// Tests that GetConfigurationKeysAsync returns only keys matching the specified pattern.
	/// </summary>
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
	/// <summary>
	/// Tests that SetConfigurationBatchAsync creates multiple configuration entries in a batch operation.
	/// Verifies all configurations are created and returns the correct count.
	/// </summary>
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
	/// <summary>
	/// Tests that ExportConfigurationAsync returns a JSON string containing all configurations.
	/// Verifies the exported string contains all key-value pairs.
	/// </summary>
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
	/// <summary>
	/// Tests that ImportConfigurationAsync imports configurations from a JSON string.
	/// Verifies all configurations are imported and persisted to database.
	/// </summary>
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
	/// <summary>
	/// Tests that ImportConfigurationAsync throws TenantConfigurationException when JSON is invalid.
	/// Verifies proper error handling for malformed JSON.
	/// </summary>
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
	/// <summary>
	/// Tests that ImportConfigurationAsync throws TenantConfigurationException when JSON is empty or null.
	/// Verifies proper error handling for invalid JSON format.
	/// </summary>
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
	/// <summary>
	/// Tests that GetStatisticsAsync returns counts of configurations including total and encrypted.
	/// Verifies statistics are calculated correctly.
	/// </summary>
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
	/// <summary>
	/// Tests that ValidateRequiredConfigurationsAsync returns true when all required configurations are present and valid.
	/// </summary>
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
	/// <summary>
	/// Tests that ValidateRequiredConfigurationsAsync returns false when required configurations are missing or empty.
	/// </summary>
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
	/// <summary>
	/// Tests that ValidateRequiredConfigurationsAsync returns true when there are no required configurations defined.
	/// </summary>
    public async Task ValidateRequiredConfigurationsAsync_WithNoRequiredConfigs_ReturnsTrue()
    {
        // Act
        var result = await _sut.ValidateRequiredConfigurationsAsync(_tenantId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
