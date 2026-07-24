#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using TenantIsolation.Models;

namespace TenantIsolation.Events;




/// <summary>
/// Marker interface for high-frequency telemetry events that may need sampling
/// when published to webhooks or other high-volume consumers
/// </summary>
public interface IHighFrequencyEvent
{
    // Marker interface - no members required
}

/// <summary>
/// Base class for all domain events in the tenant isolation system
/// Implements pub-sub pattern for cross-service communication
///
/// <para>Event Hierarchy:</para>
/// <list type="bullet">
/// <item><see cref="TenantCreatedEvent"/> - Tenant creation event</item>
/// <item><see cref="TenantActivatedEvent"/> - Tenant activation event</item>
/// <item><see cref="TenantSuspendedEvent"/> - Tenant suspension event (hard suspension)</item>
/// <item><see cref="TenantDeactivatedEvent"/> - Tenant deactivation event (soft deactivation)</item>
/// <item><see cref="TenantReactivatedEvent"/> - Tenant reactivation event</item>
/// <item><see cref="TenantDeletedEvent"/> - Tenant deletion event</item>
/// <item><see cref="TenantConfigurationChangedEvent"/> - Configuration change event</item>
/// <item><see cref="UserAddedToTenantEvent"/> - User addition event</item>
/// <item><see cref="DataIsolationPolicyChangedEvent"/> - Data isolation policy change event</item>
/// <item><see cref="FeatureToggledEvent"/> - Feature flag toggle event</item>
/// <item><see cref="TenantResourceAccessedEvent"/> - High-frequency resource access event (implements <see cref="IHighFrequencyEvent"/>)</item>
/// <item><see cref="TenantSubscriptionUpdatedEvent"/> - Subscription update event</item>
/// </list>
///
/// <para>State Machine Pattern:</para>
/// <code>Created → Activated → (Suspended|Deactivated) → Reactivated → Deleted</code>
///
/// Where:
/// <list type="bullet">
/// <item><see cref="TenantSuspendedEvent"/> - Permanent suspension (e.g., billing issues)</item>
/// <item><see cref="TenantDeactivatedEvent"/> - Soft deactivation (temporary pause)</item>
/// </list>
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
    public string? UserId { get; private set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    public void SetUserId(string? userId)
    {
        UserId = userId;
    }

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
    private const int MaxStringLength = 255;

    private string _tenantName = string.Empty;
    private string _tenantSlug = string.Empty;
    private string _adminEmail = string.Empty;
    private string _isolationStrategy = string.Empty;

    /// <summary>
    /// Tenant name
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string TenantName
    {
        get => _tenantName;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Tenant name cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _tenantName = value;
        }
    }

    /// <summary>
    /// Tenant slug identifier
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string TenantSlug
    {
        get => _tenantSlug;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Tenant slug cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _tenantSlug = value;
        }
    }

    /// <summary>
    /// Administrator email address
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string AdminEmail
    {
        get => _adminEmail;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Admin email cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _adminEmail = value;
        }
    }

    /// <summary>
    /// Isolation strategy identifier
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string IsolationStrategy
    {
        get => _isolationStrategy;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Isolation strategy cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _isolationStrategy = value;
        }
    }

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
    private string? _suspensionReason;

    /// <summary>
    /// Reason for suspension
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value exceeds maximum length.</exception>
    public string? SuspensionReason
    {
        get => _suspensionReason;
        set
        {
            if (value?.Length > 255)
            {
                throw new ArgumentException("Suspension reason cannot exceed 255 characters.", nameof(value));
            }
            _suspensionReason = value;
        }
    }

    public DateTime SuspendedAt { get; set; }

    public TenantSuspendedEvent()
    {
        Source = nameof(TenantSuspendedEvent);
    }
}

/// <summary>
/// Event when tenant is deactivated (soft-deleted/suspended from active use)
/// Completes the state machine: Created → Activated → Deactivated → Reactivated → Deleted
/// </summary>
public class TenantDeactivatedEvent : TenantEvent
{
    private string? _deactivationReason;

    /// <summary>
    /// Reason for deactivation
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value exceeds maximum length.</exception>
    public string? DeactivationReason
    {
        get => _deactivationReason;
        set
        {
            if (value?.Length > 255)
            {
                throw new ArgumentException("Deactivation reason cannot exceed 255 characters.", nameof(value));
            }
            _deactivationReason = value;
        }
    }

    /// <summary>
    /// When the tenant was deactivated
    /// </summary>
    public DateTime DeactivatedAt { get; set; }

    public TenantDeactivatedEvent()
    {
        Source = nameof(TenantDeactivatedEvent);
    }
}

/// <summary>
/// Event when tenant is reactivated after being deactivated
/// Completes the state machine: Created → Activated → Deactivated → Reactivated → Deleted
/// </summary>
public class TenantReactivatedEvent : TenantEvent
{
    private string? _reactivationReason;

    /// <summary>
    /// Reason for reactivation
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value exceeds maximum length.</exception>
    public string? ReactivationReason
    {
        get => _reactivationReason;
        set
        {
            if (value?.Length > 255)
            {
                throw new ArgumentException("Reactivation reason cannot exceed 255 characters.", nameof(value));
            }
            _reactivationReason = value;
        }
    }

    /// <summary>
    /// When the tenant was reactivated
    /// </summary>
    public DateTime ReactivatedAt { get; set; }

    public TenantReactivatedEvent()
    {
        Source = nameof(TenantReactivatedEvent);
    }
}

/// <summary>
/// Event when tenant is deleted
/// </summary>
public class TenantDeletedEvent : TenantEvent
{
    private string? _deletionReason;

    /// <summary>
    /// Reason for deletion
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value exceeds maximum length.</exception>
    public string? DeletionReason
    {
        get => _deletionReason;
        set
        {
            if (value?.Length > 255)
            {
                throw new ArgumentException("Deletion reason cannot exceed 255 characters.", nameof(value));
            }
            _deletionReason = value;
        }
    }

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
    private const int MaxStringLength = 255;
    private Dictionary<string, object> _changedProperties = new();

    /// <summary>
    /// Changed properties dictionary
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when dictionary contains keys exceeding maximum length.</exception>
    public Dictionary<string, object> ChangedProperties
    {
        get => _changedProperties;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            foreach (var key in value.Keys)
            {
                if (key.Length > MaxStringLength)
                {
                    throw new ArgumentException($"Configuration key '{key}' cannot exceed {MaxStringLength} characters.", nameof(value));
                }
            }

            _changedProperties = value;
        }
    }

    public DateTime ChangedAt { get; set; }

    public TenantConfigurationChangedEvent()
    {
        Source = nameof(TenantConfigurationChangedEvent);
        ChangedProperties = new Dictionary<string, object>();
    }
}

/// <summary>
/// Event when user is added to tenant
/// </summary>
public class UserAddedToTenantEvent : TenantEvent
{
    private const int MaxStringLength = 255;

    private string _newUserId = string.Empty;
    private string _userEmail = string.Empty;
    private string _role = string.Empty;

    /// <summary>
    /// New user identifier
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string NewUserId
    {
        get => _newUserId;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"User ID cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _newUserId = value;
        }
    }

    /// <summary>
    /// User email address
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string UserEmail
    {
        get => _userEmail;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"User email cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _userEmail = value;
        }
    }

    /// <summary>
    /// User role
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string Role
    {
        get => _role;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Role cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _role = value;
        }
    }

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
    private const int MaxStringLength = 255;

    private string _policyType = string.Empty;
    private string _oldPolicy = string.Empty;
    private string _newPolicy = string.Empty;

    /// <summary>
    /// Policy type identifier
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string PolicyType
    {
        get => _policyType;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Policy type cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _policyType = value;
        }
    }

    /// <summary>
    /// Old policy value
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when value exceeds maximum length or when value is not a valid serialized DataIsolationPolicy.
    /// </exception>
    public string OldPolicy
    {
        get => _oldPolicy;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Old policy cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _oldPolicy = value;
            ValidatePolicyString(value, nameof(OldPolicy));
        }
    }

    /// <summary>
    /// New policy value
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when value exceeds maximum length or when value is not a valid serialized DataIsolationPolicy.
    /// </exception>
    public string NewPolicy
    {
        get => _newPolicy;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"New policy cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _newPolicy = value;
            ValidatePolicyString(value, nameof(NewPolicy));
        }
    }

    public DateTime ChangedAt { get; set; }

    public DataIsolationPolicyChangedEvent()
    {
        Source = nameof(DataIsolationPolicyChangedEvent);
    }

    /// <summary>
    /// Validates that a policy string represents a valid DataIsolationPolicy.
    /// </summary>
    /// <param name="policyJson">The JSON string representation of a DataIsolationPolicy.</param>
    /// <param name="paramName">The name of the property being validated.</param>
    /// <exception cref="ArgumentException">Thrown when the policy string is not valid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyJson"/> is null.</exception>
    private static void ValidatePolicyString(string policyJson, string paramName)
    {
        try
        {
            var policy = JsonSerializer.Deserialize<DataIsolationPolicy>(policyJson);
            if (policy == null)
            {
                throw new ArgumentException("Policy string cannot deserialize to a valid DataIsolationPolicy.", paramName);
            }

            policy.EnsureValid();
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Policy string is not valid JSON or cannot be deserialized to DataIsolationPolicy.", paramName, ex);
        }
    }
}

/// <summary>
/// Event when feature flag is toggled
/// </summary>
public class FeatureToggledEvent : TenantEvent
{
    private const int MaxStringLength = 255;

    private string _featureName = string.Empty;

    /// <summary>
    /// Feature name
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string FeatureName
    {
        get => _featureName;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Feature name cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _featureName = value;
        }
    }

    public bool IsEnabled { get; set; }

    public DateTime ToggledAt { get; set; }

    public FeatureToggledEvent()
    {
        Source = nameof(FeatureToggledEvent);
    }
}

/// <summary>
/// Event for tenant resource access
/// Implements IHighFrequencyEvent for telemetry sampling
/// </summary>
public class TenantResourceAccessedEvent : TenantEvent, IHighFrequencyEvent
{
    private const int MaxStringLength = 255;
    private const string PathTraversalPattern = "../";

    private string _resourceType = string.Empty;
    private string _resourceId = string.Empty;
    private string _action = string.Empty;

    /// <summary>
    /// Resource type
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string ResourceType
    {
        get => _resourceType;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Resource type cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _resourceType = value;
        }
    }

    /// <summary>
    /// Resource identifier - validated against path traversal sequences
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty, exceeds maximum length, or contains path traversal sequences.</exception>
    public string ResourceId
    {
        get => _resourceId;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Resource ID cannot exceed {MaxStringLength} characters.", nameof(value));
            }

            // Check for path traversal sequences to prevent directory traversal attacks
            if (value.Contains(PathTraversalPattern, StringComparison.Ordinal) ||
                value.Contains("..\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Resource ID cannot contain path traversal sequences ('../' or '..\\').", nameof(value));
            }

            _resourceId = value;
        }
    }

    /// <summary>
    /// Action performed on resource
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string Action
    {
        get => _action;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Action cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _action = value;
        }
    }

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
    private const int MaxStringLength = 255;

    private string _subscriptionPlan = string.Empty;

    /// <summary>
    /// Subscription plan identifier
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or exceeds maximum length.</exception>
    public string SubscriptionPlan
    {
        get => _subscriptionPlan;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
            if (value.Length > MaxStringLength)
            {
                throw new ArgumentException($"Subscription plan cannot exceed {MaxStringLength} characters.", nameof(value));
            }
            _subscriptionPlan = value;
        }
    }

    public DateTime ExpiryDate { get; set; }
    public decimal Price { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TenantSubscriptionUpdatedEvent()
    {
        Source = nameof(TenantSubscriptionUpdatedEvent);
    }
}