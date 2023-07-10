#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Configuration;

/// <summary>
/// Configuration validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}

/// <summary>
/// Configuration validator for ensuring required settings are present and valid
/// Validates on startup to catch configuration issues early
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validate application configuration
    /// </summary>
    ValidationResult Validate();

    /// <summary>
    /// Validate specific configuration section
    /// </summary>
    ValidationResult ValidateSection(string sectionName);

    /// <summary>
    /// Throw exception if configuration is invalid
    /// </summary>
    void ValidateAndThrow();
}

/// <summary>
/// Configuration validator implementation
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(IConfiguration configuration, ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ValidationResult Validate()
    {
        var result = new ValidationResult { IsValid = true };

        _logger.LogInformation("Validating application configuration");

        // Validate database configuration
        ValidateDatabase(result);

        // Validate tenant configuration
        ValidateTenant(result);

        // Validate feature flags
        ValidateFeatures(result);

        // Validate external integrations
        ValidateIntegrations(result);

        result.IsValid = result.Errors.Count == 0;

        if (result.Errors.Count > 0)
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors",
                result.Errors.Count);
        }
        else
        {
            _logger.LogInformation("Configuration validation successful");
        }

        return result;
    }

    public ValidationResult ValidateSection(string sectionName)
    {
        // Fix: Validate sectionName parameter to prevent null or whitespace issues when accessing configuration.
        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("Section name cannot be null or whitespace.", nameof(sectionName));

        var result = new ValidationResult { IsValid = true };

        var section = _configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            result.AddError($"Configuration section '{sectionName}' not found");
            result.IsValid = false;
        }

        return result;
    }

    public void ValidateAndThrow()
    {
        var result = Validate();

        if (!result.IsValid)
        {
            var errorMessage = string.Join(Environment.NewLine, result.Errors);
            throw new InvalidOperationException(
                $"Configuration validation failed:{Environment.NewLine}{errorMessage}");
        }

        if (result.Warnings.Count > 0)
        {
            _logger.LogWarning("Configuration warnings detected: {WarningCount}",
                result.Warnings.Count);
        }
    }

    /// <summary>
    /// Validate database configuration
    /// </summary>
    private void ValidateDatabase(ValidationResult result)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.AddError("Connection string 'DefaultConnection' is not configured");
        }
        else if (connectionString.Length < 10)
        {
            result.AddError("Connection string appears to be invalid or incomplete");
        }
    }

    /// <summary>
    /// Validate tenant configuration
    /// </summary>
    private void ValidateTenant(ValidationResult result)
    {
        var section = _configuration.GetSection("TenantIsolation");

        if (!section.Exists())
        {
            result.AddWarning("TenantIsolation configuration section not found. Using defaults.");
            return;
        }

        var autoMigrate = section["AutoMigrate"];
        var enableAudit = section["EnableAuditLogging"];
        var enableSoftDelete = section["EnableSoftDeleteFilter"];

        if (string.IsNullOrEmpty(autoMigrate))
            result.AddWarning("AutoMigrate setting not configured");

        if (string.IsNullOrEmpty(enableAudit))
            result.AddWarning("EnableAuditLogging setting not configured");

        if (string.IsNullOrEmpty(enableSoftDelete))
            result.AddWarning("EnableSoftDeleteFilter setting not configured");
    }

    /// <summary>
    /// Validate feature flags
    /// </summary>
    private void ValidateFeatures(ValidationResult result)
    {
        var section = _configuration.GetSection("Features");

        if (!section.Exists())
        {
            result.AddWarning("Features configuration section not found");
            return;
        }

        var requiredFeatures = new[] { "EnableWebhooks", "EnableCaching", "EnableEventBus" };

        foreach (var feature in requiredFeatures)
        {
            var value = section[feature];
            if (string.IsNullOrEmpty(value))
                result.AddWarning($"Feature flag '{feature}' not configured");
        }
    }

    /// <summary>
    /// Validate external integrations
    /// </summary>
    private void ValidateIntegrations(ValidationResult result)
    {
        var section = _configuration.GetSection("Integration");

        if (!section.Exists())
        {
            result.AddWarning("Integration configuration section not found");
            return;
        }

        // Validate webhook configuration if enabled
        var webhookUrl = section["WebhookUrl"];
        if (!string.IsNullOrEmpty(webhookUrl) && !Uri.IsWellFormedUriString(webhookUrl, UriKind.Absolute))
        {
            result.AddError($"Invalid webhook URL: {webhookUrl}");
        }

        // Validate external API endpoints
        var externalApiUrl = section["ExternalApiUrl"];
        if (!string.IsNullOrEmpty(externalApiUrl) && !Uri.IsWellFormedUriString(externalApiUrl, UriKind.Absolute))
        {
            result.AddError($"Invalid external API URL: {externalApiUrl}");
        }
    }
}

/// <summary>
/// Extension method to register configuration validator
/// </summary>
public static class ConfigurationValidatorExtensions
{
    public static IServiceCollection AddConfigurationValidator(this IServiceCollection services)
    {
        services.AddScoped<IConfigurationValidator, ConfigurationValidator>();
        return services;
    }

    /// <summary>
    /// Validate configuration at startup
    /// </summary>
    public static IApplicationBuilder ValidateConfigurationOnStartup(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IConfigurationValidator>();
        validator.ValidateAndThrow();

        return app;
    }
}
