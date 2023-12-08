#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Service for tenant configuration management with caching
/// </summary>
public class ConfigurationService
{
    private readonly TenantDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(1);

    public ConfigurationService(TenantDbContext context, IMemoryCache cache, ILogger<ConfigurationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Set configuration value
    /// </summary>
    public async Task<TenantConfiguration> SetConfigurationAsync(
        Guid tenantId,
        string key,
        string value,
        string valueType = "string",
        bool isEncrypted = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new TenantConfigurationException(key, "Configuration key cannot be empty");

        var config = await _context.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key);

        if (config == null)
        {
            config = new TenantConfiguration
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = key,
                Value = value,
                ValueType = valueType,
                IsEncrypted = isEncrypted
            };

            await _context.TenantConfigurations.AddAsync(config);
        }
        else
        {
            config.Value = value;
            config.ValueType = valueType;
            config.IsEncrypted = isEncrypted;
            config.ModifiedAt = DateTime.UtcNow;
            _context.TenantConfigurations.Update(config);
        }

        if (!config.IsValid(out var error))
            throw new TenantConfigurationException(key, error ?? "Configuration is invalid");

        await _context.SaveChangesAsync();

        // Invalidate cache - must include the key so the per-configuration cache entry
        // (e.g. "config_{tenantId}_{key}") is cleared, not just the "all configurations" entry.
        InvalidateCache(tenantId, key);

        _logger.LogInformation("Configuration set for tenant {TenantId}: {Key}", tenantId, key);

        return config;
    }

    /// <summary>
    /// Get configuration value
    /// </summary>
    public async Task<TenantConfiguration?> GetConfigurationAsync(Guid tenantId, string key)
    {
        var cacheKey = $"config_{tenantId}_{key}";

        if (_cache.TryGetValue(cacheKey, out TenantConfiguration? cached))
            return cached;

        var config = await _context.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key);

        if (config != null)
            _cache.Set(cacheKey, config, _cacheExpiry);

        return config;
    }

    /// <summary>
    /// Get configuration value with type conversion
    /// </summary>
    public async Task<T?> GetConfigurationAsync<T>(Guid tenantId, string key)
    {
        var config = await GetConfigurationAsync(tenantId, key);
        if (config == null)
            return default;

        try
        {
            return config.GetValueAs<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert configuration {Key} to type {Type}", key, typeof(T).Name);
            throw new TenantConfigurationException(key, $"Failed to convert value to {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Get configuration or default
    /// </summary>
    public async Task<T> GetConfigurationAsync<T>(Guid tenantId, string key, T defaultValue)
    {
        var config = await GetConfigurationAsync(tenantId, key);
        if (config == null)
            return defaultValue;

        try
        {
            return config.GetValueAs<T>() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Delete configuration
    /// </summary>
    public async Task<bool> DeleteConfigurationAsync(Guid tenantId, string key)
    {
        var config = await _context.TenantConfigurations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key);

        if (config == null)
            return false;

        _context.TenantConfigurations.Remove(config);
        await _context.SaveChangesAsync();

        InvalidateCache(tenantId, key);

        _logger.LogInformation("Configuration deleted for tenant {TenantId}: {Key}", tenantId, key);

        return true;
    }

    /// <summary>
    /// Get all configurations for tenant
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllConfigurationsAsync(Guid tenantId)
    {
        var cacheKey = $"config_all_{tenantId}";

        if (_cache.TryGetValue(cacheKey, out Dictionary<string, string>? cached))
            return cached;

        var configs = await _context.TenantConfigurations
            .Where(c => c.TenantId == tenantId)
            .ToDictionaryAsync(c => c.Key, c => c.Value);

        _cache.Set(cacheKey, configs, _cacheExpiry);

        return configs;
    }

    /// <summary>
    /// Check if configuration key exists
    /// </summary>
    public async Task<bool> HasConfigurationAsync(Guid tenantId, string key)
    {
        return await _context.TenantConfigurations
            .AnyAsync(c => c.TenantId == tenantId && c.Key == key);
    }

    /// <summary>
    /// Get configuration keys by pattern
    /// </summary>
    public async Task<List<string>> GetConfigurationKeysAsync(Guid tenantId, string pattern = "*")
    {
        var allConfigs = await GetAllConfigurationsAsync(tenantId);

        if (pattern == "*")
            return allConfigs.Keys.ToList();

        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$");

        return allConfigs.Keys.Where(k => regex.IsMatch(k)).ToList();
    }

    /// <summary>
    /// Batch set configurations
    /// </summary>
    public async Task<int> SetConfigurationBatchAsync(
        Guid tenantId,
        Dictionary<string, (string value, string type, bool encrypted)> configurations)
    {
        int count = 0;

        foreach (var (key, (value, type, encrypted)) in configurations)
        {
            await SetConfigurationAsync(tenantId, key, value, type, encrypted);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Export configuration
    /// </summary>
    public async Task<string> ExportConfigurationAsync(Guid tenantId)
    {
        var configs = await GetAllConfigurationsAsync(tenantId);
        var json = System.Text.Json.JsonSerializer.Serialize(configs);
        return json;
    }

    /// <summary>
    /// Import configuration
    /// </summary>
    public async Task<int> ImportConfigurationAsync(Guid tenantId, string jsonConfig)
    {
        try
        {
            var imported = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonConfig)
                ?? throw new TenantConfigurationException("Import", "Invalid JSON format");

            int count = 0;
            foreach (var (key, value) in imported)
            {
                await SetConfigurationAsync(tenantId, key, value);
                count++;
            }

            return count;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new TenantConfigurationException("Import", $"Failed to parse JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidate cache for tenant
    /// </summary>
    private void InvalidateCache(Guid tenantId, string? key = null)
    {
        if (key != null)
        {
            var cacheKey = $"config_{tenantId}_{key}";
            _cache.Remove(cacheKey);
        }

        _cache.Remove($"config_all_{tenantId}");
    }

    /// <summary>
    /// Get configuration statistics
    /// </summary>
    public async Task<object> GetStatisticsAsync(Guid tenantId)
    {
        var configs = await _context.TenantConfigurations
            .Where(c => c.TenantId == tenantId)
            .ToListAsync();

        return new
        {
            TotalConfigurations = configs.Count,
            EncryptedConfigurations = configs.Count(c => c.IsEncrypted),
            RequiredConfigurations = configs.Count(c => c.IsRequired),
            ByValueType = configs.GroupBy(c => c.ValueType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList()
        };
    }

    /// <summary>
    /// Validate all required configurations exist
    /// </summary>
    public async Task<bool> ValidateRequiredConfigurationsAsync(Guid tenantId)
    {
        var requiredMissing = await _context.TenantConfigurations
            .Where(c => c.TenantId == tenantId && c.IsRequired && string.IsNullOrWhiteSpace(c.Value))
            .ToListAsync();

        return requiredMissing.Count == 0;
    }
}
