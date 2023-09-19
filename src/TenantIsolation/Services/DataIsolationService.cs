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
/// Service for enforcing data isolation policies
/// </summary>
public class DataIsolationService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DataIsolationService> _logger;

    public DataIsolationService(TenantDbContext context, ILogger<DataIsolationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create data isolation policy for entity type
    /// </summary>
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
    /// Get policy for entity type
    /// </summary>
    public async Task<DataIsolationPolicy?> GetPolicyAsync(Guid tenantId, string entityType)
    {
        return await _context.DataIsolationPolicies
            .Where(p => p.TenantId == tenantId && p.EntityType == entityType && p.IsActive)
            .OrderBy(p => p.Priority)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Check if field access is allowed
    /// </summary>
    public async Task<bool> IsFieldAccessAllowedAsync(Guid tenantId, string entityType, string fieldName)
    {
        var policy = await GetPolicyAsync(tenantId, entityType);
        if (policy == null)
            return true; // No policy = full access

        return policy.IsFieldAccessAllowed(fieldName);
    }

    /// <summary>
    /// Verify field access with exception
    /// </summary>
    public async Task VerifyFieldAccessAsync(Guid tenantId, string entityType, string fieldName)
    {
        if (!await IsFieldAccessAllowedAsync(tenantId, entityType, fieldName))
            throw new DataIsolationViolationException(tenantId, entityType,
                $"Access to field '{fieldName}' is denied");
    }

    /// <summary>
    /// Check cross-tenant access permission
    /// </summary>
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
    /// Update isolation policy
    /// </summary>
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
    /// Delete isolation policy
    /// </summary>
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
    /// Get all active policies for tenant
    /// </summary>
    public async Task<List<DataIsolationPolicy>> GetActivePoliciesAsync(Guid tenantId)
    {
        return await _context.DataIsolationPolicies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    /// <summary>
    /// Enable/disable policy
    /// </summary>
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
    /// Update policy priority
    /// </summary>
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
    /// Check policy violations for entity
    /// </summary>
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
    /// Export policy configuration
    /// </summary>
    public async Task<string> ExportPolicyAsync(Guid policyId)
    {
        var policy = await _context.DataIsolationPolicies.FindAsync(policyId);
        if (policy == null)
            throw new TenantIsolationException("Policy not found");

        var json = System.Text.Json.JsonSerializer.Serialize(policy);
        return json;
    }

    /// <summary>
    /// Import policy configuration
    /// </summary>
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
