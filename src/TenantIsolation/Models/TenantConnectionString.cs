#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenantIsolation.Models;

/// <summary>
/// Manages database connection strings for each tenant
/// </summary>
public class TenantConnectionString
{
    /// <summary>
    /// Unique identifier for this connection string record
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Associated tenant identifier
    /// </summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Database type (SqlServer, PostgreSQL, MySQL, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string DatabaseType { get; set; } = "SqlServer";

    /// <summary>
    /// Connection string (should be encrypted in storage)
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Friendly name for this connection
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Schema name (for schema-per-tenant strategy)
    /// </summary>
    [StringLength(128)]
    public string? SchemaName { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    [StringLength(128)]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Server hostname
    /// </summary>
    [StringLength(255)]
    public string? ServerHost { get; set; }

    /// <summary>
    /// Server port
    /// </summary>
    public int? ServerPort { get; set; }

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 300;

    /// <summary>
    /// Maximum pool size for connection pooling
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Whether to use connection pooling
    /// </summary>
    public bool UseConnectionPooling { get; set; } = true;

    /// <summary>
    /// Is this the primary connection for the tenant
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// Is this connection string active/usable
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this connection was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this connection was last tested
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of last connection test (null = not tested, true = success, false = failed)
    /// </summary>
    public bool? LastTestResult { get; set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Build a test connection string with timeout options
    /// </summary>
    public string GetTestConnectionString()
    {
        // Return connection string with shorter timeout for testing
        return ConnectionString.Replace(
            $"Connection Timeout={ConnectionTimeout}",
            "Connection Timeout=5");
    }

    /// <summary>
    /// Extract hostname from connection string
    /// </summary>
    public string ExtractHostname()
    {
        if (!string.IsNullOrEmpty(ServerHost))
            return ServerHost;

        var parts = ConnectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase) ||
                part.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Split('=')[1].Trim();
            }
        }

        return "unknown";
    }

    /// <summary>
    /// Validate connection string format
    /// </summary>
    public bool IsValidConnectionString(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            errorMessage = "Connection string cannot be empty";
            return false;
        }

        if (ConnectionString.Length < 20)
        {
            errorMessage = "Connection string appears to be invalid (too short)";
            return false;
        }

        if (DatabaseType == "SqlServer" && !ConnectionString.Contains("Server=") && !ConnectionString.Contains("Data Source="))
        {
            errorMessage = "SQL Server connection string must contain Server or Data Source parameter";
            return false;
        }

        if (ConnectionTimeout < 5 || ConnectionTimeout > 300)
        {
            errorMessage = "Connection timeout must be between 5 and 300 seconds";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Record successful connection test
    /// </summary>
    public void RecordSuccessfulTest()
    {
        LastTestedAt = DateTime.UtcNow;
        LastTestResult = true;
    }

    /// <summary>
    /// Record failed connection test
    /// </summary>
    public void RecordFailedTest()
    {
        LastTestedAt = DateTime.UtcNow;
        LastTestResult = false;
        IsActive = false;
    }
}
