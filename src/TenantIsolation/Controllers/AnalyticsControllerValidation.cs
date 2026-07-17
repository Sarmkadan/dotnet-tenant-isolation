#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Validation helpers for AnalyticsController models
// Provides comprehensive validation for health status, tenant activity, usage statistics, and error metrics
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TenantIsolation.Controllers;

/// <summary>
/// Validation helpers for AnalyticsController return types
/// Provides comprehensive validation for health status, tenant activity, usage statistics, and error metrics
/// </summary>
public static class AnalyticsControllerValidation
{
    /// <summary>
    /// Health status model
    /// </summary>
    public class HealthStatus
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    }

    /// <summary>
    /// Component health model
    /// </summary>
    public class ComponentHealth
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ResponseTimeMs { get; set; }
    }

    /// <summary>
    /// Tenant activity metrics
    /// </summary>
    public class TenantActivityMetrics
    {
        public Guid TenantId { get; set; }
        public int ActiveUsers { get; set; }
        public int RequestsPerHour { get; set; }
        public decimal DataProcessedGb { get; set; }
        public decimal StorageUsedGb { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    /// <summary>
    /// Usage statistics
    /// </summary>
    public class UsageStatistics
    {
        public string Period { get; set; } = string.Empty;
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public int AverageResponseTimeMs { get; set; }
        public int P95ResponseTimeMs { get; set; }
        public int P99ResponseTimeMs { get; set; }
        public int UniqueUsers { get; set; }
        public List<EndpointUsage> TopEndpoints { get; set; } = new();
    }

    /// <summary>
    /// Endpoint usage statistics
    /// </summary>
    public class EndpointUsage
    {
        public string Endpoint { get; set; } = string.Empty;
        public long Requests { get; set; }
        public int AverageResponseMs { get; set; }
    }

    /// <summary>
    /// Error metrics
    /// </summary>
    public class ErrorMetrics
    {
        public string Period { get; set; } = string.Empty;
        public long TotalErrors { get; set; }
        public decimal ErrorRate { get; set; }
        public List<ErrorDetail> TopErrors { get; set; } = new();
        public List<string> MostAffectedEndpoints { get; set; } = new();
    }

    /// <summary>
    /// Error detail
    /// </summary>
    public class ErrorDetail
    {
        public string ErrorType { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurred { get; set; }
    }

    /// <summary>
    /// Validates a HealthStatus instance
    /// </summary>
    /// <param name="value">The HealthStatus to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this HealthStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Status))
        {
            problems.Add("HealthStatus.Status cannot be null or whitespace");
        }
        else if (value.Status.Length > 20)
        {
            problems.Add("HealthStatus.Status exceeds maximum length of 20 characters");
        }

        if (value.Timestamp == default)
        {
            problems.Add("HealthStatus.Timestamp cannot be default (DateTime.MinValue)");
        }
        else if (value.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("HealthStatus.Timestamp cannot be in the future");
        }

        if (value.Components == null)
        {
            problems.Add("HealthStatus.Components cannot be null");
        }
        else if (value.Components.Count == 0)
        {
            problems.Add("HealthStatus.Components cannot be empty");
        }
        else
        {
            foreach (var component in value.Components.Values)
            {
                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    problems.Add("ComponentHealth.Name cannot be null or whitespace");
                }

                if (string.IsNullOrWhiteSpace(component.Status))
                {
                    problems.Add("ComponentHealth.Status cannot be null or whitespace");
                }
                else if (component.Status.Length > 20)
                {
                    problems.Add("ComponentHealth.Status exceeds maximum length of 20 characters");
                }

                if (component.ResponseTimeMs < 0)
                {
                    problems.Add("ComponentHealth.ResponseTimeMs cannot be negative");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a TenantActivityMetrics instance
    /// </summary>
    /// <param name="value">The TenantActivityMetrics to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this TenantActivityMetrics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.TenantId == Guid.Empty)
        {
            problems.Add("TenantActivityMetrics.TenantId cannot be empty Guid");
        }

        if (value.ActiveUsers < 0)
        {
            problems.Add("TenantActivityMetrics.ActiveUsers cannot be negative");
        }

        if (value.RequestsPerHour < 0)
        {
            problems.Add("TenantActivityMetrics.RequestsPerHour cannot be negative");
        }

        if (value.DataProcessedGb < 0)
        {
            problems.Add("TenantActivityMetrics.DataProcessedGb cannot be negative");
        }

        if (value.StorageUsedGb < 0)
        {
            problems.Add("TenantActivityMetrics.StorageUsedGb cannot be negative");
        }

        if (value.LastActivityAt == default)
        {
            problems.Add("TenantActivityMetrics.LastActivityAt cannot be default (DateTime.MinValue)");
        }
        else if (value.LastActivityAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("TenantActivityMetrics.LastActivityAt cannot be in the future");
        }

        if (string.IsNullOrWhiteSpace(value.Period))
        {
            problems.Add("TenantActivityMetrics.Period cannot be null or whitespace");
        }
        else if (value.Period.Length > 10)
        {
            problems.Add("TenantActivityMetrics.Period exceeds maximum length of 10 characters");
        }
        else if (!IsValidTimePeriod(value.Period))
        {
            problems.Add($"TenantActivityMetrics.Period '{value.Period}' is not a valid time period. Expected format like '1h', '24h', '7d', '30d'");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a UsageStatistics instance
    /// </summary>
    /// <param name="value">The UsageStatistics to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this UsageStatistics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Period))
        {
            problems.Add("UsageStatistics.Period cannot be null or whitespace");
        }
        else if (value.Period.Length > 10)
        {
            problems.Add("UsageStatistics.Period exceeds maximum length of 10 characters");
        }
        else if (!IsValidTimePeriod(value.Period))
        {
            problems.Add($"UsageStatistics.Period '{value.Period}' is not a valid time period. Expected format like '1h', '24h', '7d', '30d'");
        }

        if (value.TotalRequests < 0)
        {
            problems.Add("UsageStatistics.TotalRequests cannot be negative");
        }

        if (value.SuccessfulRequests < 0)
        {
            problems.Add("UsageStatistics.SuccessfulRequests cannot be negative");
        }

        if (value.FailedRequests < 0)
        {
            problems.Add("UsageStatistics.FailedRequests cannot be negative");
        }

        if (value.TotalRequests < value.SuccessfulRequests + value.FailedRequests)
        {
            problems.Add("UsageStatistics.TotalRequests cannot be less than the sum of SuccessfulRequests and FailedRequests");
        }

        if (value.AverageResponseTimeMs < 0)
        {
            problems.Add("UsageStatistics.AverageResponseTimeMs cannot be negative");
        }

        if (value.P95ResponseTimeMs < 0)
        {
            problems.Add("UsageStatistics.P95ResponseTimeMs cannot be negative");
        }

        if (value.P99ResponseTimeMs < 0)
        {
            problems.Add("UsageStatistics.P99ResponseTimeMs cannot be negative");
        }

        if (value.UniqueUsers < 0)
        {
            problems.Add("UsageStatistics.UniqueUsers cannot be negative");
        }

        if (value.TopEndpoints == null)
        {
            problems.Add("UsageStatistics.TopEndpoints cannot be null");
        }
        else if (value.TopEndpoints.Count > 100)
        {
            problems.Add("UsageStatistics.TopEndpoints exceeds maximum count of 100");
        }
        else
        {
            foreach (var endpoint in value.TopEndpoints)
            {
                if (string.IsNullOrWhiteSpace(endpoint.Endpoint))
                {
                    problems.Add("EndpointUsage.Endpoint cannot be null or whitespace");
                }
                else if (endpoint.Endpoint.Length > 100)
                {
                    problems.Add("EndpointUsage.Endpoint exceeds maximum length of 100 characters");
                }

                if (endpoint.Requests < 0)
                {
                    problems.Add("EndpointUsage.Requests cannot be negative");
                }

                if (endpoint.AverageResponseMs < 0)
                {
                    problems.Add("EndpointUsage.AverageResponseMs cannot be negative");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates an ErrorMetrics instance
    /// </summary>
    /// <param name="value">The ErrorMetrics to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this ErrorMetrics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Period))
        {
            problems.Add("ErrorMetrics.Period cannot be null or whitespace");
        }
        else if (value.Period.Length > 10)
        {
            problems.Add("ErrorMetrics.Period exceeds maximum length of 10 characters");
        }
        else if (!IsValidTimePeriod(value.Period))
        {
            problems.Add($"ErrorMetrics.Period '{value.Period}' is not a valid time period. Expected format like '1h', '24h', '7d', '30d'");
        }

        if (value.TotalErrors < 0)
        {
            problems.Add("ErrorMetrics.TotalErrors cannot be negative");
        }

        if (value.ErrorRate < 0 || value.ErrorRate > 1)
        {
            problems.Add("ErrorMetrics.ErrorRate must be between 0 and 1 inclusive");
        }

        if (value.TopErrors == null)
        {
            problems.Add("ErrorMetrics.TopErrors cannot be null");
        }
        else if (value.TopErrors.Count > 50)
        {
            problems.Add("ErrorMetrics.TopErrors exceeds maximum count of 50");
        }
        else
        {
            foreach (var error in value.TopErrors)
            {
                if (string.IsNullOrWhiteSpace(error.ErrorType))
                {
                    problems.Add("ErrorDetail.ErrorType cannot be null or whitespace");
                }
                else if (error.ErrorType.Length > 50)
                {
                    problems.Add("ErrorDetail.ErrorType exceeds maximum length of 50 characters");
                }

                if (error.Count < 0)
                {
                    problems.Add("ErrorDetail.Count cannot be negative");
                }

                if (error.LastOccurred == default)
                {
                    problems.Add("ErrorDetail.LastOccurred cannot be default (DateTime.MinValue)");
                }
                else if (error.LastOccurred > DateTime.UtcNow.AddMinutes(5))
                {
                    problems.Add("ErrorDetail.LastOccurred cannot be in the future");
                }
            }
        }

        if (value.MostAffectedEndpoints == null)
        {
            problems.Add("ErrorMetrics.MostAffectedEndpoints cannot be null");
        }
        else if (value.MostAffectedEndpoints.Count > 20)
        {
            problems.Add("ErrorMetrics.MostAffectedEndpoints exceeds maximum count of 20");
        }
        else
        {
            foreach (var endpoint in value.MostAffectedEndpoints)
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    problems.Add("ErrorMetrics.MostAffectedEndpoints contains null or whitespace entry");
                }
                else if (endpoint.Length > 100)
                {
                    problems.Add("ErrorMetrics.MostAffectedEndpoints entry exceeds maximum length of 100 characters");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a string represents a valid time period
    /// Expected formats: '1h', '24h', '7d', '30d', '1h30m', etc.
    /// </summary>
    /// <param name="period">The period string to validate</param>
    /// <returns>True if valid period format, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when period is null</exception>
    private static bool IsValidTimePeriod(string period)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (string.IsNullOrWhiteSpace(period))
            return false;

        // Check for valid format: digits followed by unit (h, d, m, s)
        var validUnits = new[] { "h", "d", "m", "s" };
        var hasValidUnit = false;

        foreach (var unit in validUnits)
        {
            if (period.EndsWith(unit, StringComparison.Ordinal))
            {
                hasValidUnit = true;
                break;
            }
        }

        if (!hasValidUnit)
            return false;

        // Extract the numeric part and validate it's a positive number
        var numericPart = period.AsSpan(0, period.Length - 1);
        return !numericPart.IsEmpty && int.TryParse(numericPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0;
    }

    /// <summary>
    /// Determines whether the specified HealthStatus is valid
    /// </summary>
    /// <param name="value">The HealthStatus to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(this HealthStatus value) => value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified TenantActivityMetrics is valid
    /// </summary>
    /// <param name="value">The TenantActivityMetrics to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(this TenantActivityMetrics value) => value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified UsageStatistics is valid
    /// </summary>
    /// <param name="value">The UsageStatistics to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(this UsageStatistics value) => value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified ErrorMetrics is valid
    /// </summary>
    /// <param name="value">The ErrorMetrics to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(this ErrorMetrics value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified HealthStatus is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The HealthStatus to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is not valid</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void EnsureValid(this HealthStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"HealthStatus is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified TenantActivityMetrics is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The TenantActivityMetrics to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is not valid</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void EnsureValid(this TenantActivityMetrics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"TenantActivityMetrics is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified UsageStatistics is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The UsageStatistics to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is not valid</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void EnsureValid(this UsageStatistics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"UsageStatistics is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the specified ErrorMetrics is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The ErrorMetrics to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is not valid</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void EnsureValid(this ErrorMetrics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ErrorMetrics is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}