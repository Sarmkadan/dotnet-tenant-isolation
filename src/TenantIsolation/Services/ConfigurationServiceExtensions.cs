#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Services;

/// <summary>
/// Extension methods for ConfigurationService providing common configuration patterns
/// </summary>
public static class ConfigurationServiceExtensions
{
    /// <summary>
    /// Get boolean configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The boolean configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<bool> GetBooleanAsync(this ConfigurationService service, Guid tenantId, string key, bool defaultValue = false)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<bool?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get integer configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The integer configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<int> GetIntAsync(this ConfigurationService service, Guid tenantId, string key, int defaultValue = 0)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<int?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get double configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The double configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<double> GetDoubleAsync(this ConfigurationService service, Guid tenantId, string key, double defaultValue = 0.0)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<double?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get float configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The float configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<float> GetFloatAsync(this ConfigurationService service, Guid tenantId, string key, float defaultValue = 0.0f)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<float?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get long configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The long configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<long> GetLongAsync(this ConfigurationService service, Guid tenantId, string key, long defaultValue = 0L)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<long?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get decimal configuration value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value to return if configuration is not found</param>
    /// <returns>The decimal configuration value or default</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<decimal> GetDecimalAsync(this ConfigurationService service, Guid tenantId, string key, decimal defaultValue = 0.0m)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<decimal?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Set configuration with automatic type detection based on value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="isEncrypted">Whether the value should be stored encrypted</param>
    /// <returns>The created or updated configuration</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/>, <paramref name="key"/>, or <paramref name="value"/> is null</exception>
    public static async Task<TenantConfiguration> SetConfigurationAutoAsync(this ConfigurationService service, Guid tenantId, string key, object value, bool isEncrypted = false)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        string valueType = value switch
        {
            bool => "bool",
            int => "int",
            double => "double",
            float => "float",
            long => "long",
            decimal => "decimal",
            _ => "string"
        };

        string stringValue = value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        return await service.SetConfigurationAsync(tenantId, key, stringValue, valueType, isEncrypted);
    }

    /// <summary>
    /// Check if configuration exists and has a non-empty value
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <returns>True if configuration exists and has a non-empty value; otherwise false</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    public static async Task<bool> HasValueAsync(this ConfigurationService service, Guid tenantId, string key)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var config = await service.GetConfigurationAsync(tenantId, key);
        return config != null && !string.IsNullOrWhiteSpace(config.Value);
    }

    /// <summary>
    /// Get configuration value or throw if not found
    /// </summary>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <returns>The configuration entity</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    /// <exception cref="TenantConfigurationException">Configuration not found for the specified key</exception>
    public static async Task<TenantConfiguration> GetConfigurationOrThrowAsync(this ConfigurationService service, Guid tenantId, string key)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var config = await service.GetConfigurationAsync(tenantId, key);
        return config ?? throw new TenantConfigurationException(key, "Configuration not found");
    }

    /// <summary>
    /// Get typed configuration value or throw if not found
    /// </summary>
    /// <typeparam name="T">The type to deserialize the configuration value to</typeparam>
    /// <param name="service">The configuration service instance</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="key">The configuration key</param>
    /// <returns>The deserialized configuration value</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is null</exception>
    /// <exception cref="TenantConfigurationException">Configuration not found or invalid for the specified key</exception>
    public static async Task<T> GetAsync<T>(this ConfigurationService service, Guid tenantId, string key)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await service.GetConfigurationAsync<T>(tenantId, key);
        return value ?? throw new TenantConfigurationException(key, $"Configuration '{key}' not found or invalid");
    }
}