#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenantIsolation.Constants;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Service for enforcing data isolation policies.
/// </summary>
public class DataIsolationService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DataIsolationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationService"/> class.
    /// </summary>
    /// <param name="context">The tenant database context.</param>
    /// <param name="logger">The logger instance.</param>
    public DataIsolationService(TenantDbContext context, ILogger<DataIsolationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a data isolation policy for a given entity type.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="entityType">The type of the entity to apply the policy to.</param>
    /// <param name="policyType">The type of the data isolation policy.</param>
    /// <returns>The created <see cref="DataIsolationPolicy"/>.</returns>
    /// <exception cref="TenantIsolationException">Thrown when the policy configuration is invalid.</exception>
    public async Task<DataIsolationPolicy> CreatePolicyAsync(
        Guid tenantId,
        string entityType,
        DataIsolationPolicyType policyType = DataIsolationPolicyType.Strict)
    {
        var policy = new DataIsolationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            PolicyType = policyType,
            IsActive = true
        };

        if (!policy.IsValidPolicy(out var error))
            throw new TenantIsolationException($"Invalid policy: {error}");

        await _context.DataIsolationPolicies.AddAsync(policy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created data isolation policy {PolicyId} for tenant {TenantId}",
            policy.Id, tenantId);

        return policy;
    }

    /// <summary>
    /// Gets the data isolation policy for a specified entity type and tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <returns>The <see cref="DataIsolationPolicy"/> if found; otherwise, <c>null</c>.</returns>
    public async Task<DataIsolationPolicy?> GetPolicyAsync(Guid tenantId, string entityType)
    {
        return await _context.DataIsolationPolicies
            .Where(p => p.TenantId == tenantId && p.EntityType == entityType && p.IsActive)
            .OrderBy(p => p.Priority)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Checks if field access is allowed based on the tenant's data isolation policy.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns><c>true</c> if access is allowed; otherwise, <c>false</c>.</returns>
    public async Task<bool> IsFieldAccessAllowedAsync(Guid tenantId, string entityType, string fieldName)
    {
        var policy = await GetPolicyAsync(tenantId, entityType);
        if (policy == null)
            return true; // No policy = full access

        return policy.IsFieldAccessAllowed(fieldName);
    }

    /// <summary>
    /// Verifies field access and throws an exception if access is denied.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <exception cref="DataIsolationViolationException">Thrown when field access is denied.</exception>
    public async Task VerifyFieldAccessAsync(Guid tenantId, string entityType, string fieldName)
    {
        if (!await IsFieldAccessAllowedAsync(tenantId, entityType, fieldName))
            throw new DataIsolationViolationException(tenantId, entityType,
                $"Access to field '{fieldName}' is denied");
    }

    /// <summary>
    /// Checks if cross-tenant access is allowed for the specified entity type.
    /// </summary>
    /// <param name="currentTenantId">The identifier of the current tenant.</param>
    /// <param name="targetTenantId">The identifier of the target tenant.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <returns><c>true</c> if cross-tenant access is allowed; otherwise, <c>false</c>.</returns>
    public async Task<bool> CanAccessCrossTenantAsync(Guid currentTenantId, Guid targetTenantId, string entityType)
    {
        var policy = await GetPolicyAsync(currentTenantId, entityType);
        if (policy == null)
            return false; // No policy = strict isolation

        if (policy.PolicyType == DataIsolationPolicyType.Strict)
            return false;

        return policy.IsCrossTenantAccessAllowed(targetTenantId);
    }

    /// <summary>
    /// Updates an existing isolation policy.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="updateAction">The action to apply updates to the policy.</param>
    /// <returns>The updated <see cref="DataIsolationPolicy"/>.</returns>
    /// <exception cref="TenantIsolationException">Thrown when the policy is not found or the update results in an invalid policy.</exception>
    public async Task<DataIsolationPolicy> UpdatePolicyAsync(
        Guid policyId,
        Action<DataIsolationPolicy> updateAction)
    {
        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            throw new TenantIsolationException("Policy not found");

        updateAction(policy);
        policy.UpdatedAt = DateTime.UtcNow;

        if (!policy.IsValidPolicy(out var error))
            throw new TenantIsolationException($"Invalid policy update: {error}");

        _context.DataIsolationPolicies.Update(policy);
        await _context.SaveChangesAsync();

        return policy;
    }

    /// <summary>
    /// Deletes an isolation policy by its identifier.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <returns><c>true</c> if the policy was deleted; otherwise, <c>false</c>.</returns>
    public async Task<bool> DeletePolicyAsync(Guid policyId)
    {
        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            return false;

        _context.DataIsolationPolicies.Remove(policy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted data isolation policy {PolicyId}", policyId);
        return true;
    }

    /// <summary>
    /// Gets all active isolation policies for a tenant.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>A list of active <see cref="DataIsolationPolicy"/>.</returns>
    public async Task<List<DataIsolationPolicy>> GetActivePoliciesAsync(Guid tenantId)
    {
        return await _context.DataIsolationPolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    /// <summary>
    /// Enables or disables an isolation policy.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="isActive">Indicates whether the policy should be active.</param>
    /// <returns><c>true</c> if the policy was updated; otherwise, <c>false</c>.</returns>
    public async Task<bool> SetPolicyActiveAsync(Guid policyId, bool isActive)
    {
        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            return false;

        policy.IsActive = isActive;
        policy.UpdatedAt = DateTime.UtcNow;
        _context.DataIsolationPolicies.Update(policy);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Updates the priority of an isolation policy.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <param name="priority">The new priority level (must be between 1 and 1000).</param>
    /// <returns><c>true</c> if the policy priority was updated; otherwise, <c>false</c>.</returns>
    /// <exception cref="TenantIsolationException">Thrown when the priority is outside the allowed range.</exception>
    public async Task<bool> SetPolicyPriorityAsync(Guid policyId, int priority)
    {
        if (priority < 1 || priority > 1000)
            throw new TenantIsolationException("Priority must be between 1 and 1000");

        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            return false;

        policy.Priority = priority;
        policy.UpdatedAt = DateTime.UtcNow;
        _context.DataIsolationPolicies.Update(policy);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Checks for policy violations for a given entity data.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityData">The entity data to check.</param>
    /// <returns>A list of violation messages.</returns>
    public async Task<List<string>> CheckPolicyViolationsAsync(Guid tenantId, string entityType, object entityData)
    {
        var violations = new List<string>();
        var policy = await GetPolicyAsync(tenantId, entityType);

        if (policy == null)
            return violations; // No policy = no violations

        // Check field access violations
        var entityProperties = entityData.GetType().GetProperties();
        foreach (var prop in entityProperties)
        {
            if (!policy.IsFieldAccessAllowed(prop.Name))
                violations.Add($"Field '{prop.Name}' is not accessible");
        }

        return violations;
    }

    /// <summary>
    /// Exports the policy configuration as a JSON string.
    /// </summary>
    /// <param name="policyId">The unique identifier of the policy.</param>
    /// <returns>The serialized JSON string of the policy.</returns>
    /// <exception cref="TenantIsolationException">Thrown when the policy is not found.</exception>
    public async Task<string> ExportPolicyAsync(Guid policyId)
    {
        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            throw new TenantIsolationException("Policy not found");

        var json = System.Text.Json.JsonSerializer.Serialize(policy);
        return json;
    }

    /// <summary>
    /// Imports a policy configuration from a JSON string.
    /// </summary>
    /// <param name="jsonConfig">The JSON configuration string.</param>
    /// <param name="tenantId">The unique identifier of the tenant for the new policy.</param>
    /// <returns>The imported <see cref="DataIsolationPolicy"/>.</returns>
    /// <exception cref="TenantIsolationException">Thrown when the JSON is invalid or the policy configuration is invalid.</exception>
    public async Task<DataIsolationPolicy> ImportPolicyAsync(string jsonConfig, Guid tenantId)
    {
        try
        {
            var imported = System.Text.Json.JsonSerializer.Deserialize<DataIsolationPolicy>(jsonConfig)
                ?? throw new TenantIsolationException("Invalid policy configuration");

            imported.Id = Guid.NewGuid();
            imported.TenantId = tenantId;

            if (!imported.IsValidPolicy(out var error))
                throw new TenantIsolationException($"Invalid imported policy: {error}");

            await _context.DataIsolationPolicies.AddAsync(imported);
            await _context.SaveChangesAsync();

            return imported;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new TenantIsolationException($"Failed to parse policy JSON: {ex.Message}", ex);
        }
    }
}
