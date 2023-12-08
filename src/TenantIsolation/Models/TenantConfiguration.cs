#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Stores tenant-specific configuration settings
/// </summary>
public class TenantConfiguration
{
    /// <summary>
    /// Unique identifier for this configuration record
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Associated tenant identifier
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Configuration key (e.g., "features:api:enabled")
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Key { get; set; } = null!;

    /// <summary>
    /// Configuration value stored as string/JSON
    /// </summary>
    [Required]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Optional description of what this configuration controls
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Value type for deserialization (string, int, bool, json, etc.)
    /// </summary>
    [StringLength(50)]
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// Whether this configuration is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Is this configuration required for tenant operation
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this configuration can be overridden at runtime
    /// </summary>
    public bool IsOverridable { get; set; } = true;

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this configuration was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Parse configuration value to specified type
    /// </summary>
    public T? GetValueAs<T>()
    {
        return ValueType.ToLowerInvariant() switch
        {
            "bool" => (T)(object)bool.Parse(Value),
            "int" => (T)(object)int.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
            "long" => (T)(object)long.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
            "decimal" => (T)(object)decimal.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
            "double" => (T)(object)double.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
            "json" => System.Text.Json.JsonSerializer.Deserialize<T>(Value),
            _ => (T)(object)Value
        };
    }

    /// <summary>
    /// Set configuration value from typed object
    /// </summary>
    public void SetValue<T>(T value)
    {
        if (value == null)
        {
            Value = "";
            return;
        }

        Value = value switch
        {
            bool b => b.ToString(),
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string s => s,
            _ => System.Text.Json.JsonSerializer.Serialize(value)
        };

        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validate configuration against business rules
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(Key))
        {
            errorMessage = "Configuration key cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Value) && IsRequired)
        {
            errorMessage = "Required configuration value cannot be empty";
            return false;
        }

        if (IsEncrypted && Value.Length < 16)
        {
            errorMessage = "Encrypted values must meet minimum length requirements";
            return false;
        }

        return true;
    }
}
