using FluentAssertions;
using TenantIsolation.Events;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantEventExtensionsTests
{
    [Fact]
    public void GetEventDescription_WithValidTenantEvent_ReturnsFormattedDescription()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantCreatedEvent = new TenantCreatedEvent
        {
            TenantName = "Test Tenant",
            TenantSlug = "test-tenant"
        };

        // Set TenantId using reflection since it's protected set
        var tenantIdProperty = typeof(TenantEvent).GetProperty("TenantId");
        tenantIdProperty?.SetValue(tenantCreatedEvent, tenantId);

        // Act
        var description = tenantCreatedEvent.GetEventDescription();

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().StartWith("Event [TenantCreatedEvent]");
        description.Should().Contain(tenantCreatedEvent.EventId);
        description.Should().Contain(tenantId.ToString());
        description.Should().Contain("OccurredAt:");
    }

    [Fact]
    public void GetEventDescription_WithTenantActivatedEvent_ReturnsCorrectTypeName()
    {
        // Arrange
        var tenantActivatedEvent = new TenantActivatedEvent();

        // Act
        var description = tenantActivatedEvent.GetEventDescription();

        // Assert
        description.Should().StartWith("Event [TenantActivatedEvent]");
    }

    [Fact]
    public void GetEventDescription_WithTenantSuspendedEvent_ReturnsCorrectTypeName()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Act
        var description = tenantSuspendedEvent.GetEventDescription();

        // Assert
        description.Should().StartWith("Event [TenantSuspendedEvent]");
    }

    [Fact]
    public void GetEventDescription_WithTenantDeletedEvent_ReturnsCorrectTypeName()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var description = tenantDeletedEvent.GetEventDescription();

        // Assert
        description.Should().StartWith("Event [TenantDeletedEvent]");
    }

    [Fact]
    public void GetEventDescription_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        TenantEvent? nullEvent = null;

        // Act
        Action act = () => nullEvent!.GetEventDescription();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsTenantActivatedEvent_WithTenantActivatedEvent_ReturnsTrue()
    {
        // Arrange
        var tenantActivatedEvent = new TenantActivatedEvent();

        // Act
        var result = tenantActivatedEvent.IsTenantActivatedEvent();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTenantActivatedEvent_WithNonActivatedEvent_ReturnsFalse()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var result1 = tenantCreatedEvent.IsTenantActivatedEvent();
        var result2 = tenantSuspendedEvent.IsTenantActivatedEvent();
        var result3 = tenantDeletedEvent.IsTenantActivatedEvent();

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public void IsTenantActivatedEvent_WithNullEvent_ReturnsFalse()
    {
        // Arrange
        TenantEvent? nullEvent = null;

        // Act
        Action act = () => nullEvent!.IsTenantActivatedEvent();

        // Assert - pattern matching with null returns false, doesn't throw
        act.Should().NotThrow();
        nullEvent.IsTenantActivatedEvent().Should().BeFalse();
    }

    [Fact]
    public void IsTenantSuspendedEvent_WithTenantSuspendedEvent_ReturnsTrue()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Act
        var result = tenantSuspendedEvent.IsTenantSuspendedEvent();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTenantSuspendedEvent_WithNonSuspendedEvent_ReturnsFalse()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var result1 = tenantCreatedEvent.IsTenantSuspendedEvent();
        var result2 = tenantActivatedEvent.IsTenantSuspendedEvent();
        var result3 = tenantDeletedEvent.IsTenantSuspendedEvent();

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public void IsTenantSuspendedEvent_WithNullEvent_ReturnsFalse()
    {
        // Arrange
        TenantEvent? nullEvent = null;

        // Act
        Action act = () => nullEvent!.IsTenantSuspendedEvent();

        // Assert - pattern matching with null returns false, doesn't throw
        act.Should().NotThrow();
        nullEvent.IsTenantSuspendedEvent().Should().BeFalse();
    }

    [Fact]
    public void IsTenantDeletedEvent_WithTenantDeletedEvent_ReturnsTrue()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var result = tenantDeletedEvent.IsTenantDeletedEvent();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTenantDeletedEvent_WithNonDeletedEvent_ReturnsFalse()
    {
        // Arrange
        var tenantCreatedEvent = new TenantCreatedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Act
        var result1 = tenantCreatedEvent.IsTenantDeletedEvent();
        var result2 = tenantActivatedEvent.IsTenantDeletedEvent();
        var result3 = tenantSuspendedEvent.IsTenantDeletedEvent();

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public void IsTenantDeletedEvent_WithNullEvent_ReturnsFalse()
    {
        // Arrange
        TenantEvent? nullEvent = null;

        // Act
        Action act = () => nullEvent!.IsTenantDeletedEvent();

        // Assert - pattern matching with null returns false, doesn't throw
        act.Should().NotThrow();
        nullEvent.IsTenantDeletedEvent().Should().BeFalse();
    }

    [Fact]
    public void IsTenantActivatedEvent_IsSpecificToEventType()
    {
        // Arrange
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var activatedResult = tenantActivatedEvent.IsTenantActivatedEvent();
        var suspendedResult = tenantSuspendedEvent.IsTenantActivatedEvent();
        var deletedResult = tenantDeletedEvent.IsTenantActivatedEvent();

        // Assert
        activatedResult.Should().BeTrue();
        suspendedResult.Should().BeFalse();
        deletedResult.Should().BeFalse();
    }

    [Fact]
    public void IsTenantSuspendedEvent_IsSpecificToEventType()
    {
        // Arrange
        var tenantSuspendedEvent = new TenantSuspendedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantDeletedEvent = new TenantDeletedEvent();

        // Act
        var suspendedResult = tenantSuspendedEvent.IsTenantSuspendedEvent();
        var activatedResult = tenantActivatedEvent.IsTenantSuspendedEvent();
        var deletedResult = tenantDeletedEvent.IsTenantSuspendedEvent();

        // Assert
        suspendedResult.Should().BeTrue();
        activatedResult.Should().BeFalse();
        deletedResult.Should().BeFalse();
    }

    [Fact]
    public void IsTenantDeletedEvent_IsSpecificToEventType()
    {
        // Arrange
        var tenantDeletedEvent = new TenantDeletedEvent();
        var tenantActivatedEvent = new TenantActivatedEvent();
        var tenantSuspendedEvent = new TenantSuspendedEvent();

        // Act
        var deletedResult = tenantDeletedEvent.IsTenantDeletedEvent();
        var activatedResult = tenantActivatedEvent.IsTenantDeletedEvent();
        var suspendedResult = tenantSuspendedEvent.IsTenantDeletedEvent();

        // Assert
        deletedResult.Should().BeTrue();
        activatedResult.Should().BeFalse();
        suspendedResult.Should().BeFalse();
    }
}
