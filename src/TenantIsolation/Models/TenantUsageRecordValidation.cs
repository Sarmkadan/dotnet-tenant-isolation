#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TenantIsolation.Models;

/// <summary>
/// Provides validation helpers for <see cref="TenantUsageRecord"/> instances.
/// </summary>
public static class TenantUsageRecordValidation
{
    /// <summary>
    /// Validates a <see cref="TenantUsageRecord"/> and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The record to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of validation errors</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this TenantUsageRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (value.Id == Guid.Empty)
            errors.Add("Id must be a non-empty GUID");

        // Validate TenantId
        if (value.TenantId == Guid.Empty)
            errors.Add("TenantId must be a non-empty GUID");

        // Validate MetricKey
        if (string.IsNullOrWhiteSpace(value.MetricKey))
            errors.Add("MetricKey must not be null or whitespace");
        else if (value.MetricKey.Length > 100)
            errors.Add("MetricKey must not exceed 100 characters");

        // Validate CurrentValue (should be non-negative)
        if (value.CurrentValue < 0)
            errors.Add("CurrentValue must be non-negative");

        // Validate QuotaLimit
        if (value.QuotaLimit.HasValue && value.QuotaLimit.Value < 0)
            errors.Add("QuotaLimit must be non-negative when specified");

        // Validate Period
        // UsagePeriod is an enum with valid values, no validation needed

        // Validate PeriodStart
        if (value.PeriodStart == default)
            errors.Add("PeriodStart must be a valid DateTime");
        else if (value.PeriodStart > DateTime.UtcNow.AddYears(1))
            errors.Add("PeriodStart cannot be more than one year in the future");

        // Validate ResetAt
        if (value.ResetAt.HasValue)
        {
            if (value.ResetAt.Value == default)
                errors.Add("ResetAt must be a valid DateTime when specified");
            else if (value.ResetAt.Value > DateTime.UtcNow.AddYears(1))
                errors.Add("ResetAt cannot be more than one year in the future");

            // ResetAt should not be before PeriodStart
            if (value.ResetAt.Value < value.PeriodStart)
                errors.Add("ResetAt cannot be before PeriodStart");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
            errors.Add("CreatedAt must be a valid DateTime");
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
            errors.Add("CreatedAt cannot be more than 5 minutes in the future");

        // Validate UpdatedAt
        if (value.UpdatedAt == default)
            errors.Add("UpdatedAt must be a valid DateTime");
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
            errors.Add("UpdatedAt cannot be more than 5 minutes in the future");

        // Validate that UpdatedAt is not before CreatedAt
        if (value.UpdatedAt < value.CreatedAt)
            errors.Add("UpdatedAt cannot be before CreatedAt");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="TenantUsageRecord"/> is valid.
    /// </summary>
    /// <param name="value">The record to check</param>
    /// <returns><c>true</c> if the record is valid; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this TenantUsageRecord value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="TenantUsageRecord"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The record to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the record is invalid, with a detailed message</exception>
    public static void EnsureValid(this TenantUsageRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
            throw new ArgumentException(
                $"TenantUsageRecord is invalid. Errors: {string.Join(", ", errors)}",
                nameof(value)
            );
    }
}