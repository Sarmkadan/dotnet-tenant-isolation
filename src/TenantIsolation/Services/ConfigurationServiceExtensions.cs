#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static async Task<bool> GetBooleanAsync(this ConfigurationService service, Guid tenantId, string key, bool defaultValue = false)
    {
        var value = await service.GetConfigurationAsync<bool?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get integer configuration value
    /// </summary>
    public static async Task<int> GetIntAsync(this ConfigurationService service, Guid tenantId, string key, int defaultValue = 0)
    {
        var value = await service.GetConfigurationAsync<int?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Get double configuration value
    /// </summary>
    public static async Task<double> GetDoubleAsync(this ConfigurationService service, Guid tenantId, string key, double defaultValue = 0.0)
    {
        var value = await service.GetConfigurationAsync<double?>(tenantId, key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Set configuration with automatic type detection based on value
    /// </summary>
    public static async Task<TenantConfiguration> SetConfigurationAutoAsync(this ConfigurationService service, Guid tenantId, string key, object value, bool isEncrypted = false)
    {
        string valueType;
        string stringValue;

        switch (value)
        {
            case bool _:
                valueType = "bool";
                stringValue = value.ToString().ToLowerInvariant();
                break;
            case int _:
                valueType = "int";
                stringValue = value.ToString();
                break;
            case double _:
                valueType = "double";
                stringValue = value.ToString();
                break;
            case float _:
                valueType = "float";
                stringValue = value.ToString();
                break;
            case long _:
                valueType = "long";
                stringValue = value.ToString();
                break;
            default:
                valueType = "string";
                stringValue = value.ToString() ?? string.Empty;
                break;
        }

        return await service.SetConfigurationAsync(tenantId, key, stringValue, valueType, isEncrypted);
    }

    /// <summary>
    /// Check if configuration exists and has a non-empty value
    /// </summary>
    public static async Task<bool> HasValueAsync(this ConfigurationService service, Guid tenantId, string key)
    {
        var config = await service.GetConfigurationAsync(tenantId, key);
        return config != null && !string.IsNullOrWhiteSpace(config.Value);
    }

    /// <summary>
    /// Get configuration value or throw if not found
    /// </summary>
    public static async Task<TenantConfiguration> GetConfigurationOrThrowAsync(this ConfigurationService service, Guid tenantId, string key)
    {
        var config = await service.GetConfigurationAsync(tenantId, key);
        return config ?? throw new TenantConfigurationException(key, "Configuration not found");
    }

    /// <summary>
    /// Get typed configuration value or throw if not found
    /// </summary>
    public static async Task<T> GetAsync<T>(this ConfigurationService service, Guid tenantId, string key)
    {
        var value = await service.GetConfigurationAsync<T>(tenantId, key);
        return value ?? throw new TenantConfigurationException(key, $"Configuration '{key}' not found or invalid");
    }
}