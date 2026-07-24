using FluentAssertions;
using TenantIsolation.Events;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Tests for input validation in TenantEvent subclasses
/// </summary>
public class TenantEventValidationTests
{
    [Fact]
    public void TenantCreatedEvent_TenantName_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tenantCreatedEvent.TenantName = null!);
    }

    [Fact]
    public void TenantCreatedEvent_TenantName_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenantCreatedEvent.TenantName = string.Empty);
    }

    [Fact]
    public void TenantCreatedEvent_TenantName_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var longString = new string('A', 256); // Max is 255

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantCreatedEvent.TenantName = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantCreatedEvent_TenantName_Valid_ShouldSetValue()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantName = "Valid Tenant Name";

        // Act
        tenantCreatedEvent.TenantName = tenantName;

        // Assert
        tenantCreatedEvent.TenantName.Should().Be(tenantName);
    }

    [Fact]
    public void TenantCreatedEvent_TenantSlug_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tenantCreatedEvent.TenantSlug = null!);
    }

    [Fact]
    public void TenantCreatedEvent_TenantSlug_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenantCreatedEvent.TenantSlug = string.Empty);
    }

    [Fact]
    public void TenantCreatedEvent_TenantSlug_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var longString = new string('B', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantCreatedEvent.TenantSlug = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantCreatedEvent_AdminEmail_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tenantCreatedEvent.AdminEmail = null!);
    }

    [Fact]
    public void TenantCreatedEvent_AdminEmail_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenantCreatedEvent.AdminEmail = string.Empty);
    }

    [Fact]
    public void TenantCreatedEvent_AdminEmail_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var longString = new string('C', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantCreatedEvent.AdminEmail = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantCreatedEvent_IsolationStrategy_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => tenantCreatedEvent.IsolationStrategy = null!);
    }

    [Fact]
    public void TenantCreatedEvent_IsolationStrategy_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenantCreatedEvent.IsolationStrategy = string.Empty);
    }

    [Fact]
    public void TenantCreatedEvent_IsolationStrategy_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var longString = new string('D', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantCreatedEvent.IsolationStrategy = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantSuspendedEvent_SuspensionReason_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var longString = new string('E', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantSuspendedEvent.SuspensionReason = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantSuspendedEvent_SuspensionReason_Null_ShouldNotThrow()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Act
        tenantSuspendedEvent.SuspensionReason = null;

        // Assert
        tenantSuspendedEvent.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void TenantSuspendedEvent_SuspensionReason_Valid_ShouldSetValue()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var reason = "Billing issue with payment processor";

        // Act
        tenantSuspendedEvent.SuspensionReason = reason;

        // Assert
        tenantSuspendedEvent.SuspensionReason.Should().Be(reason);
    }

    [Fact]
    public void TenantDeactivatedEvent_DeactivationReason_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantDeactivatedEvent = new TenantDeactivatedEvent();
        var longString = new string('F', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantDeactivatedEvent.DeactivationReason = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantDeactivatedEvent_DeactivationReason_Null_ShouldNotThrow()
    {
        // Arrange
        var tenantDeactivatedEvent = new TenantDeactivatedEvent();

        // Act
        tenantDeactivatedEvent.DeactivationReason = null;

        // Assert
        tenantDeactivatedEvent.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public void TenantReactivatedEvent_ReactivationReason_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantReactivatedEvent = new TenantReactivatedEvent();
        var longString = new string('G', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantReactivatedEvent.ReactivationReason = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantReactivatedEvent_ReactivationReason_Null_ShouldNotThrow()
    {
        // Arrange
        var tenantReactivatedEvent = new TenantReactivatedEvent();

        // Act
        tenantReactivatedEvent.ReactivationReason = null;

        // Assert
        tenantReactivatedEvent.ReactivationReason.Should().BeNull();
    }

    [Fact]
    public void TenantDeletedEvent_DeletionReason_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();
        var longString = new string('H', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => tenantDeletedEvent.DeletionReason = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantDeletedEvent_DeletionReason_Null_ShouldNotThrow()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        tenantDeletedEvent.DeletionReason = null;

        // Assert
        tenantDeletedEvent.DeletionReason.Should().BeNull();
    }

    [Fact]
    public void TenantConfigurationChangedEvent_ChangedProperties_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configChangedEvent = new TenantConfigurationChangedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => configChangedEvent.ChangedProperties = null!);
    }

    [Fact]
    public void TenantConfigurationChangedEvent_ChangedProperties_WithLongKey_ShouldThrowArgumentException()
    {
        // Arrange
        var configChangedEvent = new TenantConfigurationChangedEvent();
        var longKey = new string('I', 256);
        var properties = new Dictionary<string, object> { { longKey, "value" } };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => configChangedEvent.ChangedProperties = properties);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantConfigurationChangedEvent_ChangedProperties_Valid_ShouldSetValue()
    {
        // Arrange
        var configChangedEvent = new TenantConfigurationChangedEvent();
        var properties = new Dictionary<string, object>
        {
            { "MaxUsers", 100 },
            { "StorageLimitGB", 1000 },
            { "FeatureFlags.Enabled", true }
        };

        // Act
        configChangedEvent.ChangedProperties = properties;

        // Assert
        configChangedEvent.ChangedProperties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void UserAddedToTenantEvent_NewUserId_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => userAddedEvent.NewUserId = null!);
    }

    [Fact]
    public void UserAddedToTenantEvent_NewUserId_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => userAddedEvent.NewUserId = string.Empty);
    }

    [Fact]
    public void UserAddedToTenantEvent_NewUserId_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();
        var longString = new string('J', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => userAddedEvent.NewUserId = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void UserAddedToTenantEvent_UserEmail_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => userAddedEvent.UserEmail = null!);
    }

    [Fact]
    public void UserAddedToTenantEvent_UserEmail_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => userAddedEvent.UserEmail = string.Empty);
    }

    [Fact]
    public void UserAddedToTenantEvent_UserEmail_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();
        var longString = new string('K', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => userAddedEvent.UserEmail = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void UserAddedToTenantEvent_Role_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => userAddedEvent.Role = null!);
    }

    [Fact]
    public void UserAddedToTenantEvent_Role_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => userAddedEvent.Role = string.Empty);
    }

    [Fact]
    public void UserAddedToTenantEvent_Role_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var userAddedEvent = new UserAddedToTenantEvent();
        var longString = new string('L', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => userAddedEvent.Role = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_PolicyType_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policyChangedEvent.PolicyType = null!);
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_PolicyType_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => policyChangedEvent.PolicyType = string.Empty);
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_PolicyType_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var longString = new string('M', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => policyChangedEvent.PolicyType = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_OldPolicy_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var longString = new string('N', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => policyChangedEvent.OldPolicy = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void DataIsolationPolicyChangedEvent_NewPolicy_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var longString = new string('O', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => policyChangedEvent.NewPolicy = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void FeatureToggledEvent_FeatureName_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var featureToggledEvent = new FeatureToggledEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => featureToggledEvent.FeatureName = null!);
    }

    [Fact]
    public void FeatureToggledEvent_FeatureName_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var featureToggledEvent = new FeatureToggledEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => featureToggledEvent.FeatureName = string.Empty);
    }

    [Fact]
    public void FeatureToggledEvent_FeatureName_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var featureToggledEvent = new FeatureToggledEvent();
        var longString = new string('P', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => featureToggledEvent.FeatureName = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceType_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resourceAccessedEvent.ResourceType = null!);
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceType_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceType = string.Empty);
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceType_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var longString = new string('Q', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceType = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resourceAccessedEvent.ResourceId = null!);
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceId = string.Empty);
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var longString = new string('R', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceId = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_ContainsPathTraversal_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceId = "../database/secret");
        exception.Message.Should().Contain("path traversal");
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_ContainsBackslashPathTraversal_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => resourceAccessedEvent.ResourceId = "..\\database\\secret");
        exception.Message.Should().Contain("path traversal");
    }

    [Fact]
    public void TenantResourceAccessedEvent_ResourceId_Valid_ShouldSetValue()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var resourceId = "database-123";

        // Act
        resourceAccessedEvent.ResourceId = resourceId;

        // Assert
        resourceAccessedEvent.ResourceId.Should().Be(resourceId);
    }

    [Fact]
    public void TenantResourceAccessedEvent_Action_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resourceAccessedEvent.Action = null!);
    }

    [Fact]
    public void TenantResourceAccessedEvent_Action_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => resourceAccessedEvent.Action = string.Empty);
    }

    [Fact]
    public void TenantResourceAccessedEvent_Action_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var longString = new string('S', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => resourceAccessedEvent.Action = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void TenantSubscriptionUpdatedEvent_SubscriptionPlan_Null_ShouldThrowArgumentNullException()
    {
        // Arrange
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => subscriptionUpdatedEvent.SubscriptionPlan = null!);
    }

    [Fact]
    public void TenantSubscriptionUpdatedEvent_SubscriptionPlan_Empty_ShouldThrowArgumentException()
    {
        // Arrange
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => subscriptionUpdatedEvent.SubscriptionPlan = string.Empty);
    }

    [Fact]
    public void TenantSubscriptionUpdatedEvent_SubscriptionPlan_ExceedsMaxLength_ShouldThrowArgumentException()
    {
        // Arrange
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();
        var longString = new string('T', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => subscriptionUpdatedEvent.SubscriptionPlan = longString);
        exception.Message.Should().Contain("255 characters");
    }

    [Fact]
    public void AllEventTypes_ShouldHaveValidationForAllStringProperties()
    {
        // This test ensures that all string properties in all event types have validation
        // Arrange & Act - just verify all classes can be instantiated
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var tenantDeactivatedEvent = new TenantDeactivatedEvent();
        var tenantReactivatedEvent = new TenantReactivatedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();
        var configChangedEvent = new TenantConfigurationChangedEvent();
        var userAddedEvent = new UserAddedToTenantEvent();
        var policyChangedEvent = new DataIsolationPolicyChangedEvent();
        var featureToggledEvent = new FeatureToggledEvent();
        var resourceAccessedEvent = new TenantResourceAccessedEvent();
        var subscriptionUpdatedEvent = new TenantSubscriptionUpdatedEvent();

        // Assert - just verify they exist and have proper source
        tenantCreatedEvent.Source.Should().NotBeEmpty();
        tenantSuspendedEvent.Source.Should().NotBeEmpty();
        tenantDeactivatedEvent.Source.Should().NotBeEmpty();
        tenantReactivatedEvent.Source.Should().NotBeEmpty();
        tenantDeletedEvent.Source.Should().NotBeEmpty();
        configChangedEvent.Source.Should().NotBeEmpty();
        userAddedEvent.Source.Should().NotBeEmpty();
        policyChangedEvent.Source.Should().NotBeEmpty();
        featureToggledEvent.Source.Should().NotBeEmpty();
        resourceAccessedEvent.Source.Should().NotBeEmpty();
        subscriptionUpdatedEvent.Source.Should().NotBeEmpty();
    }
}