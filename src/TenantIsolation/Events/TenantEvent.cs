// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace TenantIsolation.Events;

/// <summary>
/// Base class for all domain events in the tenant isolation system
/// Implements pub-sub pattern for cross-service communication
/// </summary>
public abstract class TenantEvent
{
    /// <summary>
    /// Unique event ID for tracking
    /// </summary>
    public string EventId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant associated with event
    /// </summary>
    public Guid TenantId { get; protected set; }

    /// <summary>
    /// User who triggered event (optional)
    /// </summary>
    public string? UserId { get; protected set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Event source (which service/module triggered it)
    /// </summary>
    public string Source { get; protected set; } = string.Empty;
}

/// <summary>
/// Event when tenant is created
/// </summary>
public class TenantCreatedEvent : TenantEvent
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string IsolationStrategy { get; set; } = string.Empty;

    public TenantCreatedEvent()
    {
        Source = nameof(TenantCreatedEvent);
    }
}

/// <summary>
/// Event when tenant is activated
/// </summary>
public class TenantActivatedEvent : TenantEvent
{
    public DateTime ActivatedAt { get; set; }

    public TenantActivatedEvent()
    {
        Source = nameof(TenantActivatedEvent);
    }
}

/// <summary>
/// Event when tenant is suspended
/// </summary>
public class TenantSuspendedEvent : TenantEvent
{
    public string? SuspensionReason { get; set; }
    public DateTime SuspendedAt { get; set; }

    public TenantSuspendedEvent()
    {
        Source = nameof(TenantSuspendedEvent);
    }
}

/// <summary>
/// Event when tenant is deleted
/// </summary>
public class TenantDeletedEvent : TenantEvent
{
    public string? DeletionReason { get; set; }
    public DateTime DeletedAt { get; set; }

    public TenantDeletedEvent()
    {
        Source = nameof(TenantDeletedEvent);
    }
}

/// <summary>
/// Event when tenant configuration changes
/// </summary>
public class TenantConfigurationChangedEvent : TenantEvent
{
    public Dictionary<string, object> ChangedProperties { get; set; } = new();
    public DateTime ChangedAt { get; set; }

    public TenantConfigurationChangedEvent()
    {
        Source = nameof(TenantConfigurationChangedEvent);
    }
}

/// <summary>
/// Event when user is added to tenant
/// </summary>
public class UserAddedToTenantEvent : TenantEvent
{
    public string NewUserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }

    public UserAddedToTenantEvent()
    {
        Source = nameof(UserAddedToTenantEvent);
    }
}

/// <summary>
/// Event when data isolation policy changes
/// </summary>
public class DataIsolationPolicyChangedEvent : TenantEvent
{
    public string PolicyType { get; set; } = string.Empty;
    public string OldPolicy { get; set; } = string.Empty;
    public string NewPolicy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }

    public DataIsolationPolicyChangedEvent()
    {
        Source = nameof(DataIsolationPolicyChangedEvent);
    }
}

/// <summary>
/// Event when feature flag is toggled
/// </summary>
public class FeatureToggledEvent : TenantEvent
{
    public string FeatureName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime ToggledAt { get; set; }

    public FeatureToggledEvent()
    {
        Source = nameof(FeatureToggledEvent);
    }
}

/// <summary>
/// Event for tenant resource access
/// </summary>
public class TenantResourceAccessedEvent : TenantEvent
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public bool WasSuccessful { get; set; }

    public TenantResourceAccessedEvent()
    {
        Source = nameof(TenantResourceAccessedEvent);
    }
}

/// <summary>
/// Event for subscription updates
/// </summary>
public class TenantSubscriptionUpdatedEvent : TenantEvent
{
    public string SubscriptionPlan { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantSubscriptionUpdatedEvent()
    {
        Source = nameof(TenantSubscriptionUpdatedEvent);
    }
}
