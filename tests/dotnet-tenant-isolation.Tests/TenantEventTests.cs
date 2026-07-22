using FluentAssertions;
using TenantIsolation.Events;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantEventTests
{
    [Fact]
    public void TenantEvent_BaseProperties_InDerivedEvent_ShouldBeInitializedCorrectly()
    {
        // Act
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Assert
        tenantCreatedEvent.EventId.Should().NotBeNullOrEmpty();
        tenantCreatedEvent.EventId.Should().MatchRegex("^[a-f0-9]{32}$");
        tenantCreatedEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantCreatedEvent.TenantId.Should().Be(Guid.Empty);
        tenantCreatedEvent.UserId.Should().BeNull();
        tenantCreatedEvent.CorrelationId.Should().BeNull();
        tenantCreatedEvent.Source.Should().NotBeEmpty();
    }

    [Fact]
    public void TenantEvent_SetUserId_ShouldSetUserIdCorrectly()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var userId = "user-123";

        // Act
        tenantCreatedEvent.SetUserId(userId);

        // Assert
        tenantCreatedEvent.UserId.Should().Be(userId);
    }

    [Fact]
    public void TenantEvent_SetUserId_WithNull_ShouldClearUserId()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        tenantCreatedEvent.SetUserId("user-123");

        // Act
        tenantCreatedEvent.SetUserId(null);

        // Assert
        tenantCreatedEvent.UserId.Should().BeNull();
    }

    [Fact]
    public void TenantEvent_SetUserId_WithEmptyString_ShouldSetEmptyString()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act
        tenantCreatedEvent.SetUserId(string.Empty);

        // Assert
        tenantCreatedEvent.UserId.Should().BeEmpty();
    }

    [Fact]
    public void TenantEvent_CorrelationId_ShouldBeSettable()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var correlationId = "corr-456";

        // Act
        tenantCreatedEvent.CorrelationId = correlationId;

        // Assert
        tenantCreatedEvent.CorrelationId.Should().Be(correlationId);
    }


    [Fact]
    public void TenantEvent_Source_InDerivedEvent_ShouldBeInitialized()
    {
        // Arrange & Act
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Assert
        tenantCreatedEvent.Source.Should().NotBeEmpty();
    }

    [Fact]
    public void TenantCreatedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Assert
        tenantCreatedEvent.Source.Should().Be(nameof(TenantCreatedEvent));
        tenantCreatedEvent.TenantName.Should().BeEmpty();
        tenantCreatedEvent.TenantSlug.Should().BeEmpty();
        tenantCreatedEvent.AdminEmail.Should().BeEmpty();
        tenantCreatedEvent.IsolationStrategy.Should().BeEmpty();
    }

    [Fact]
    public void TenantCreatedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantName = "Test Tenant";
        var tenantSlug = "test-tenant";
        var adminEmail = "admin@test.com";
        var isolationStrategy = "IsolationStrategy1";

        // Act
        tenantCreatedEvent.TenantName = tenantName;
        tenantCreatedEvent.TenantSlug = tenantSlug;
        tenantCreatedEvent.AdminEmail = adminEmail;
        tenantCreatedEvent.IsolationStrategy = isolationStrategy;

        // Assert
        tenantCreatedEvent.TenantName.Should().Be(tenantName);
        tenantCreatedEvent.TenantSlug.Should().Be(tenantSlug);
        tenantCreatedEvent.AdminEmail.Should().Be(adminEmail);
        tenantCreatedEvent.IsolationStrategy.Should().Be(isolationStrategy);
    }

    [Fact]
    public void TenantCreatedEvent_AllPropertiesSet_ShouldWorkCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = "user-789";
        var correlationId = "corr-789";
        var tenantName = "Production Tenant";
        var tenantSlug = "prod-tenant";
        var adminEmail = "admin@company.com";
        var isolationStrategy = "StrongIsolation";

        // Act
        var tenantCreatedEvent = new TenantCreatedEvent
        {
            TenantName = tenantName,
            TenantSlug = tenantSlug,
            AdminEmail = adminEmail,
            IsolationStrategy = isolationStrategy
        };

        // Set base properties
        var propertyInfo = typeof(TenantEvent).GetProperty("TenantId");
        propertyInfo?.SetValue(tenantCreatedEvent, tenantId);
        tenantCreatedEvent.SetUserId(userId);
        tenantCreatedEvent.CorrelationId = correlationId;

        // Assert
        tenantCreatedEvent.EventId.Should().NotBeNullOrEmpty();
        tenantCreatedEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantCreatedEvent.TenantId.Should().Be(tenantId);
        tenantCreatedEvent.UserId.Should().Be(userId);
        tenantCreatedEvent.CorrelationId.Should().Be(correlationId);
        tenantCreatedEvent.Source.Should().Be(nameof(TenantCreatedEvent));
        tenantCreatedEvent.TenantName.Should().Be(tenantName);
        tenantCreatedEvent.TenantSlug.Should().Be(tenantSlug);
        tenantCreatedEvent.AdminEmail.Should().Be(adminEmail);
        tenantCreatedEvent.IsolationStrategy.Should().Be(isolationStrategy);
    }

    [Fact]
    public void TenantActivatedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var tenantActivatedEvent = new TenantActivatedEvent();

        // Assert
        tenantActivatedEvent.Source.Should().Be(nameof(TenantActivatedEvent));
        tenantActivatedEvent.ActivatedAt.Should().Be(default);
    }

    [Fact]
    public void TenantActivatedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var tenantActivatedEvent = new TenantActivatedEvent();
        var activatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        tenantActivatedEvent.ActivatedAt = activatedAt;

        // Assert
        tenantActivatedEvent.ActivatedAt.Should().Be(activatedAt);
    }

    [Fact]
    public void TenantSuspendedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Assert
        tenantSuspendedEvent.Source.Should().Be(nameof(TenantSuspendedEvent));
        tenantSuspendedEvent.SuspensionReason.Should().BeNull();
        tenantSuspendedEvent.SuspendedAt.Should().Be(default);
    }

    [Fact]
    public void TenantSuspendedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var suspensionReason = "Billing issue";
        var suspendedAt = DateTime.UtcNow.AddHours(-2);

        // Act
        tenantSuspendedEvent.SuspensionReason = suspensionReason;
        tenantSuspendedEvent.SuspendedAt = suspendedAt;

        // Assert
        tenantSuspendedEvent.SuspensionReason.Should().Be(suspensionReason);
        tenantSuspendedEvent.SuspendedAt.Should().Be(suspendedAt);
    }

    [Fact]
    public void TenantDeletedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Assert
        tenantDeletedEvent.Source.Should().Be(nameof(TenantDeletedEvent));
        tenantDeletedEvent.DeletionReason.Should().BeNull();
        tenantDeletedEvent.DeletedAt.Should().Be(default);
    }

    [Fact]
    public void TenantDeletedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();
        var deletionReason = "Account closed by user";
        var deletedAt = DateTime.UtcNow.AddDays(-7);

        // Act
        tenantDeletedEvent.DeletionReason = deletionReason;
        tenantDeletedEvent.DeletedAt = deletedAt;

        // Assert
        tenantDeletedEvent.DeletionReason.Should().Be(deletionReason);
        tenantDeletedEvent.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void TenantConfigurationChangedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var configChangedEvent = new TenantConfigurationChangedEvent();

        // Assert
        configChangedEvent.Source.Should().Be(nameof(TenantConfigurationChangedEvent));
        configChangedEvent.ChangedProperties.Should().NotBeNull();
        configChangedEvent.ChangedProperties.Should().BeEmpty();
        configChangedEvent.ChangedAt.Should().Be(default);
    }

    [Fact]
    public void TenantConfigurationChangedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var configChangedEvent = new TenantConfigurationChangedEvent();
        var changedProperties = new Dictionary<string, object>
        {
            { "MaxUsers", 100 },
            { "StorageLimitGB", 1000 },
            { "FeatureFlags.Enabled", true }
        };
        var changedAt = DateTime.UtcNow;

        // Act
        configChangedEvent.ChangedProperties = changedProperties;
        configChangedEvent.ChangedAt = changedAt;

        // Assert
        configChangedEvent.ChangedProperties.Should().BeEquivalentTo(changedProperties);
        configChangedEvent.ChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public void UserAddedToTenantEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var userAddedEvent = new UserAddedToTenantEvent();

        // Assert
        userAddedEvent.Source.Should().Be(nameof(UserAddedToTenantEvent));
        userAddedEvent.NewUserId.Should().BeEmpty();
        userAddedEvent.UserEmail.Should().BeEmpty();
        userAddedEvent.Role.Should().BeEmpty();
        userAddedEvent.AddedAt.Should().Be(default);
    }

    [Fact]
    public void UserAddedToTenantEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();
        var newUserId = "user-999";
        var userEmail = "newuser@company.com";
        var role = "Administrator";
        var addedAt = DateTime.UtcNow.AddMinutes(-30);

        // Act
        userAddedEvent.NewUserId = newUserId;
        userAddedEvent.UserEmail = userEmail;
        userAddedEvent.Role = role;
        userAddedEvent.AddedAt = addedAt;

        // Assert
        userAddedEvent.NewUserId.Should().Be(newUserId);
        userAddedEvent.UserEmail.Should().Be(userEmail);
        userAddedEvent.Role.Should().Be(role);
        userAddedEvent.AddedAt.Should().Be(addedAt);
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();

        // Assert
        policyChangedEvent.Source.Should().Be(nameof(DataIsolationPolicyChangedEvent));
        policyChangedEvent.PolicyType.Should().BeEmpty();
        policyChangedEvent.OldPolicy.Should().BeEmpty();
        policyChangedEvent.NewPolicy.Should().BeEmpty();
        policyChangedEvent.ChangedAt.Should().Be(default);
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var policyType = "DataAccess";
        var oldPolicy = "Shared";
        var newPolicy = "Isolated";
        var changedAt = DateTime.UtcNow.AddHours(-1);

        // Act
        policyChangedEvent.PolicyType = policyType;
        policyChangedEvent.OldPolicy = oldPolicy;
        policyChangedEvent.NewPolicy = newPolicy;
        policyChangedEvent.ChangedAt = changedAt;

        // Assert
        policyChangedEvent.PolicyType.Should().Be(policyType);
        policyChangedEvent.OldPolicy.Should().Be(oldPolicy);
        policyChangedEvent.NewPolicy.Should().Be(newPolicy);
        policyChangedEvent.ChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public void FeatureToggledEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var featureToggledEvent = new FeatureToggledEvent();

        // Assert
        featureToggledEvent.Source.Should().Be(nameof(FeatureToggledEvent));
        featureToggledEvent.FeatureName.Should().BeEmpty();
        featureToggledEvent.IsEnabled.Should().BeFalse();
        featureToggledEvent.ToggledAt.Should().Be(default);
    }

    [Fact]
    public void FeatureToggledEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var featureToggledEvent = new FeatureToggledEvent();
        var featureName = "NewDashboard";
        var isEnabled = true;
        var toggledAt = DateTime.UtcNow;

        // Act
        featureToggledEvent.FeatureName = featureName;
        featureToggledEvent.IsEnabled = isEnabled;
        featureToggledEvent.ToggledAt = toggledAt;

        // Assert
        featureToggledEvent.FeatureName.Should().Be(featureName);
        featureToggledEvent.IsEnabled.Should().Be(isEnabled);
        featureToggledEvent.ToggledAt.Should().Be(toggledAt);
    }

    [Fact]
    public void TenantResourceAccessedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Assert
        resourceAccessedEvent.Source.Should().Be(nameof(TenantResourceAccessedEvent));
        resourceAccessedEvent.ResourceType.Should().BeEmpty();
        resourceAccessedEvent.ResourceId.Should().BeEmpty();
        resourceAccessedEvent.Action.Should().BeEmpty();
        resourceAccessedEvent.AccessedAt.Should().Be(default);
        resourceAccessedEvent.WasSuccessful.Should().BeFalse();
    }

    [Fact]
    public void TenantResourceAccessedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var resourceType = "Database";
        var resourceId = "db-123";
        var action = "Read";
        var accessedAt = DateTime.UtcNow.AddSeconds(-10);
        var wasSuccessful = true;

        // Act
        resourceAccessedEvent.ResourceType = resourceType;
        resourceAccessedEvent.ResourceId = resourceId;
        resourceAccessedEvent.Action = action;
        resourceAccessedEvent.AccessedAt = accessedAt;
        resourceAccessedEvent.WasSuccessful = wasSuccessful;

        // Assert
        resourceAccessedEvent.ResourceType.Should().Be(resourceType);
        resourceAccessedEvent.ResourceId.Should().Be(resourceId);
        resourceAccessedEvent.Action.Should().Be(action);
        resourceAccessedEvent.AccessedAt.Should().Be(accessedAt);
        resourceAccessedEvent.WasSuccessful.Should().Be(wasSuccessful);
    }

    [Fact]
    public void TenantSubscriptionUpdatedEvent_ShouldInitializeWithCorrectSource()
    {
        // Act
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();

        // Assert
        subscriptionUpdatedEvent.Source.Should().Be(nameof(TenantSubscriptionUpdatedEvent));
        subscriptionUpdatedEvent.SubscriptionPlan.Should().BeEmpty();
        subscriptionUpdatedEvent.ExpiryDate.Should().Be(default);
        subscriptionUpdatedEvent.Price.Should().Be(0);
        subscriptionUpdatedEvent.UpdatedAt.Should().Be(default);
    }

    [Fact]
    public void TenantSubscriptionUpdatedEvent_Properties_ShouldBeSettable()
    {
        // Arrange
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();
        var subscriptionPlan = "Enterprise";
        var expiryDate = DateTime.UtcNow.AddMonths(1);
        var price = 99.99m;
        var updatedAt = DateTime.UtcNow;

        // Act
        subscriptionUpdatedEvent.SubscriptionPlan = subscriptionPlan;
        subscriptionUpdatedEvent.ExpiryDate = expiryDate;
        subscriptionUpdatedEvent.Price = price;
        subscriptionUpdatedEvent.UpdatedAt = updatedAt;

        // Assert
        subscriptionUpdatedEvent.SubscriptionPlan.Should().Be(subscriptionPlan);
        subscriptionUpdatedEvent.ExpiryDate.Should().Be(expiryDate);
        subscriptionUpdatedEvent.Price.Should().Be(price);
        subscriptionUpdatedEvent.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void AllEventTypes_ShouldHaveUniqueEventIds()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();
        var configChangedEvent = new TenantConfigurationChangedEvent();
        var userAddedEvent = new UserAddedToTenantEvent();
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var featureToggledEvent = new FeatureToggledEvent();
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();

        // Assert
        var eventIds = new List<string>
        {
            tenantCreatedEvent.EventId,
            tenantActivatedEvent.EventId,
            tenantSuspendedEvent.EventId,
            tenantDeletedEvent.EventId,
            configChangedEvent.EventId,
            userAddedEvent.EventId,
            policyChangedEvent.EventId,
            featureToggledEvent.EventId,
            resourceAccessedEvent.EventId,
            subscriptionUpdatedEvent.EventId
        };

        eventIds.Should().HaveCount(10);
        eventIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllEventTypes_ShouldHaveOccurredAtSetToUtcNow()
    {
        // Arrange & Act
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();

        // Small delay to ensure different timestamps
        Thread.Sleep(10);
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Assert
        tenantCreatedEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantActivatedEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenantSuspendedEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}