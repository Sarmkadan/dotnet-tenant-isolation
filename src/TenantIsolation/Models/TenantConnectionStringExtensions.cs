using System;

namespace TenantIsolation.Models
{
    /// <summary>
    /// Provides extension methods for the <see cref="TenantConnectionString"/> class.
    /// </summary>
    public static class TenantConnectionStringExtensions
    {
        /// <summary>
        /// Validates the configuration of a <see cref="TenantConnectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if any required or invalid configuration is detected.</exception>
        public static void Validate(this TenantConnectionString connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            _ = connectionString.ServerHost ?? throw new ArgumentException(
                "ServerHost must not be null or empty.", nameof(connectionString));

            _ = connectionString.ServerPort is null or <= 0 or > 65535
                ? throw new ArgumentException("ServerPort must be between 1 and 65535.", nameof(connectionString))
                : true;

            _ = connectionString.DatabaseType ?? throw new ArgumentException(
                "DatabaseType must not be null or empty.", nameof(connectionString));

            _ = connectionString.DatabaseType is "SqlServer" or "PostgreSQL" or "MySQL"
                ? true
                : throw new ArgumentException(
                    "DatabaseType must be a supported type: SqlServer, PostgreSQL, MySQL.", nameof(connectionString));

            _ = !string.IsNullOrEmpty(connectionString.SchemaName) && connectionString.SchemaName.Length > 128
                ? throw new ArgumentException("SchemaName must not exceed 128 characters.", nameof(connectionString))
                : true;
        }

        /// <summary>
        /// Generates a human-readable display name for the <see cref="TenantConnectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to generate a display name for.</param>
        /// <returns>A formatted string representing the connection string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionString"/> is null.</exception>
        public static string ToDisplayName(this TenantConnectionString connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            return string.IsNullOrEmpty(connectionString.Name)
                ? $"{connectionString.ServerHost}:{connectionString.ServerPort} ({connectionString.DatabaseName})"
                : $"{connectionString.Name} ({connectionString.ServerHost}:{connectionString.ServerPort})";
        }
    }
}