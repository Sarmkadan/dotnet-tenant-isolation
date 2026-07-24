#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Default <see cref="IDataIsolationPolicyValidator"/> implementation backed by
/// <see cref="TenantDbContext"/>. Field-level rules mirror the historical behaviour of
/// the static <c>DataIsolationPolicyValidation</c> helper, extended with structured error
/// codes and database-aware checks (connection string presence, cross-policy conflicts).
/// </summary>
public sealed class DataIsolationPolicyValidator : IDataIsolationPolicyValidator
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DataIsolationPolicyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationPolicyValidator"/> class.
    /// </summary>
    /// <param name="context">The database context used for connection-string and sibling-policy lookups.</param>
    /// <param name="logger">Logger used to record validation outcomes.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> or <paramref name="logger"/> is null.</exception>
    public DataIsolationPolicyValidator(TenantDbContext context, ILogger<DataIsolationPolicyValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public PolicyValidationResult ValidateFields(DataIsolationPolicy policy) => ValidateFieldsStatic(policy);

    /// <summary>
    /// Validates the intrinsic field-level shape of a policy without requiring an instance of
    /// this validator or a database context. Exposed for the static
    /// <see cref="DataIsolationPolicyValidation"/> compatibility shim.
    /// </summary>
    /// <param name="policy">The policy to validate.</param>
    /// <returns>The structured field-level validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    internal static PolicyValidationResult ValidateFieldsStatic(DataIsolationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return PolicyValidationResult.Failure(EnumerateFieldErrors(policy));
    }

    /// <inheritdoc />
    public async Task<PolicyValidationResult> ValidateAsync(DataIsolationPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var errors = new List<PolicyValidationError>(EnumerateFieldErrors(policy));

        var hasActiveConnectionString = await _context.TenantConnectionStrings
            .AsNoTracking()
            .AnyAsync(c => c.TenantId == policy.TenantId && c.IsActive, cancellationToken);

        if (!hasActiveConnectionString)
        {
            errors.Add(new PolicyValidationError(
                PolicyErrorCode.MissingConnectionString,
                $"Tenant {policy.TenantId} has no active connection string configured, so isolation policy '{policy.EntityType}' cannot be enforced at the data-access layer.",
                nameof(DataIsolationPolicy.TenantId)));
        }

        if (policy.IsActive)
        {
            var conflictingSibling = await _context.DataIsolationPolicies
                .AsNoTracking()
                .Where(p => p.Id != policy.Id
                    && p.TenantId == policy.TenantId
                    && p.EntityType == policy.EntityType
                    && p.IsActive
                    && p.PolicyType != policy.PolicyType)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (conflictingSibling.HasValue)
            {
                errors.Add(new PolicyValidationError(
                    PolicyErrorCode.ConflictingIsolationMode,
                    $"Policy {policy.Id} ({policy.PolicyType}) conflicts with active sibling policy {conflictingSibling.Value} for the same tenant/entity, which declares a different isolation mode.",
                    nameof(DataIsolationPolicy.PolicyType)));
            }
        }

        var result = PolicyValidationResult.Failure(errors);

        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Data isolation policy {PolicyId} for tenant {TenantId}/{EntityType} failed validation with {ErrorCount} error(s): {Errors}",
                policy.Id, policy.TenantId, policy.EntityType, result.Errors.Count, result.ToDisplayString());
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PolicyValidationReport> ValidateAllAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _context.DataIsolationPolicies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var entries = new List<PolicyValidationReportEntry>(policies.Count);

        foreach (var policy in policies)
        {
            var result = await ValidateAsync(policy, cancellationToken);
            entries.Add(new PolicyValidationReportEntry(policy.Id, policy.TenantId, policy.EntityType, result));
        }

        var report = new PolicyValidationReport(entries);

        if (report.IsValid)
        {
            _logger.LogInformation("Validated {PolicyCount} data isolation policies, all valid.", entries.Count);
        }
        else
        {
            _logger.LogError("Data isolation policy validation found {FailedCount} of {TotalCount} policies invalid.",
                report.FailedEntries.Count, entries.Count);
        }

        return report;
    }

    /// <summary>
    /// Computes the field-level (database-independent) validation errors for a policy.
    /// </summary>
    private static IEnumerable<PolicyValidationError> EnumerateFieldErrors(DataIsolationPolicy value)
    {
        if (value.Id == Guid.Empty)
        {
            yield return new PolicyValidationError(PolicyErrorCode.MissingId, "Id must be a non-empty GUID", nameof(DataIsolationPolicy.Id));
        }

        if (value.TenantId == Guid.Empty)
        {
            yield return new PolicyValidationError(PolicyErrorCode.MissingTenantId, "TenantId must be a non-empty GUID", nameof(DataIsolationPolicy.TenantId));
        }

        if (string.IsNullOrWhiteSpace(value.EntityType))
        {
            yield return new PolicyValidationError(PolicyErrorCode.MissingEntityType, "EntityType is required", nameof(DataIsolationPolicy.EntityType));
        }
        else if (value.EntityType.Length > 100)
        {
            yield return new PolicyValidationError(PolicyErrorCode.EntityTypeTooLong, "EntityType must be 100 characters or less", nameof(DataIsolationPolicy.EntityType));
        }

        if (!Enum.IsDefined(typeof(DataIsolationPolicyType), value.PolicyType))
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidPolicyType, "PolicyType must be a valid DataIsolationPolicyType value", nameof(DataIsolationPolicy.PolicyType));
        }

        if (value.Priority is < 0 or > 1000)
        {
            yield return new PolicyValidationError(PolicyErrorCode.PriorityOutOfRange, "Priority must be between 0 and 1000", nameof(DataIsolationPolicy.Priority));
        }

        if (value.CreatedAt == default)
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidCreatedAt, "CreatedAt must be set to a valid DateTime", nameof(DataIsolationPolicy.CreatedAt));
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidCreatedAt, "CreatedAt cannot be in the future", nameof(DataIsolationPolicy.CreatedAt));
        }

        if (value.UpdatedAt == default)
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidUpdatedAt, "UpdatedAt must be set to a valid DateTime", nameof(DataIsolationPolicy.UpdatedAt));
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidUpdatedAt, "UpdatedAt cannot be in the future", nameof(DataIsolationPolicy.UpdatedAt));
        }

        if (value.PolicyType == DataIsolationPolicyType.Custom && string.IsNullOrWhiteSpace(value.FilterRule))
        {
            yield return new PolicyValidationError(PolicyErrorCode.MissingFilterRule, "FilterRule is required for Custom policy type", nameof(DataIsolationPolicy.FilterRule));
        }

        if (!string.IsNullOrWhiteSpace(value.AllowedFields) && value.GetAllowedFields().Any(f => string.IsNullOrWhiteSpace(f)))
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidFieldList, "AllowedFields contains empty or whitespace field names", nameof(DataIsolationPolicy.AllowedFields));
        }

        if (!string.IsNullOrWhiteSpace(value.DeniedFields) && value.GetDeniedFields().Any(f => string.IsNullOrWhiteSpace(f)))
        {
            yield return new PolicyValidationError(PolicyErrorCode.InvalidFieldList, "DeniedFields contains empty or whitespace field names", nameof(DataIsolationPolicy.DeniedFields));
        }

        if (!string.IsNullOrWhiteSpace(value.AllowedCrossTenantAccess))
        {
            var tenants = value.AllowedCrossTenantAccess.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tenants.Any(tenant => !Guid.TryParse(tenant.Trim(), out _)))
            {
                yield return new PolicyValidationError(PolicyErrorCode.InvalidCrossTenantAccessFormat, "AllowedCrossTenantAccess contains invalid GUID format", nameof(DataIsolationPolicy.AllowedCrossTenantAccess));
            }
        }

        var deniedFieldsList = value.GetDeniedFields();
        var allowedFieldsList = value.GetAllowedFields();

        if (deniedFieldsList.Count > 0 && allowedFieldsList.Count > 0)
        {
            var overlap = deniedFieldsList.Intersect(allowedFieldsList, StringComparer.OrdinalIgnoreCase).ToList();
            if (overlap.Count > 0)
            {
                yield return new PolicyValidationError(
                    PolicyErrorCode.ConflictingFieldRules,
                    $"Fields cannot be in both allowed and denied lists: {string.Join(", ", overlap)}",
                    nameof(DataIsolationPolicy.AllowedFields));
            }
        }

        if (!string.IsNullOrWhiteSpace(value.Description) && value.Description.Length > 1000)
        {
            yield return new PolicyValidationError(PolicyErrorCode.DescriptionTooLong, "Description must be 1000 characters or less", nameof(DataIsolationPolicy.Description));
        }

        if (!string.IsNullOrWhiteSpace(value.FilterRule) && value.FilterRule.Length > 10000)
        {
            yield return new PolicyValidationError(PolicyErrorCode.FilterRuleTooLong, "FilterRule must be 10000 characters or less", nameof(DataIsolationPolicy.FilterRule));
        }

        if (value.PolicyType == DataIsolationPolicyType.Strict && !string.IsNullOrWhiteSpace(value.AllowedCrossTenantAccess))
        {
            yield return new PolicyValidationError(
                PolicyErrorCode.ConflictingIsolationMode,
                "PolicyType is Strict but AllowedCrossTenantAccess is populated; Strict policies never permit cross-tenant access",
                nameof(DataIsolationPolicy.AllowedCrossTenantAccess));
        }
    }
}
