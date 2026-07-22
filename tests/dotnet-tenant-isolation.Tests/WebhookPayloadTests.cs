using FluentAssertions;
using Xunit;
using TenantIsolation.Integration;

namespace TenantIsolation.Tests;

public class WebhookPayloadTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializePropertiesWithDefaultValues()
    {
        // Act
        var payload = new WebhookPayload();

        // Assert
        payload.EventId.Should().NotBeNull();
        payload.EventType.Should().BeEmpty();
        payload.TenantId.Should().Be(Guid.Empty);
        payload.Timestamp.Should().BeWithin(TimeSpan.FromSeconds(2)).After(DateTime.UtcNow.AddSeconds(-2));
        payload.Data.Should().BeNull();
        payload.Signature.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString("N");
        var eventType = "TenantCreatedEvent";
        var tenantId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var data = new { Name = "Test Tenant", Status = "Active" };
        var signature = "sha256-hmac-signature";

        // Act
        var payload = new WebhookPayload
        {
            EventId = eventId,
            EventType = eventType,
            TenantId = tenantId,
            Timestamp = timestamp,
            Data = data,
            Signature = signature
        };

        // Assert
        payload.EventId.Should().Be(eventId);
        payload.EventType.Should().Be(eventType);
        payload.TenantId.Should().Be(tenantId);
        payload.Timestamp.Should().Be(timestamp);
        payload.Data.Should().BeEquivalentTo(data);
        payload.Signature.Should().Be(signature);
    }

    [Fact]
    public void EventId_ShouldBeSetToValidGuidString()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        var eventId = Guid.NewGuid().ToString("N");
        payload.EventId = eventId;

        // Assert
        payload.EventId.Should().Be(eventId);
        payload.EventId.Should().MatchRegex("^[a-f0-9]{32}$");
    }

    [Fact]
    public void EventId_WithEmptyString_ShouldSetEmptyString()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        payload.EventId = string.Empty;

        // Assert
        payload.EventId.Should().BeEmpty();
    }

    [Fact]
    public void EventType_ShouldBeSetToValidEventType()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        var eventType = "TenantSuspendedEvent";
        payload.EventType = eventType;

        // Assert
        payload.EventType.Should().Be(eventType);
    }

    [Fact]
    public void EventType_WithEmptyString_ShouldSetEmptyString()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        payload.EventType = string.Empty;

        // Assert
        payload.EventType.Should().BeEmpty();
    }

    [Fact]
    public void TenantId_ShouldBeSetToValidGuid()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        var tenantId = Guid.NewGuid();
        payload.TenantId = tenantId;

        // Assert
        payload.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_WithEmptyGuid_ShouldSetEmptyGuid()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        payload.TenantId = Guid.Empty;

        // Assert
        payload.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Timestamp_ShouldBeSetToSpecificDateTime()
    {
        // Arrange
        var payload = new WebhookPayload();
        var timestamp = DateTime.UtcNow.AddDays(-1);

        // Act
        payload.Timestamp = timestamp;

        // Assert
        payload.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void Data_ShouldBeSetToNull()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        payload.Data = null;

        // Assert
        payload.Data.Should().BeNull();
    }

    [Fact]
    public void Data_ShouldBeSetToComplexObject()
    {
        // Arrange
        var payload = new WebhookPayload();
        var complexData = new
        {
            TenantName = "Production Tenant",
            TenantSlug = "prod-tenant",
            AdminEmail = "admin@company.com",
            IsolationStrategy = "StrongIsolation",
            UserCount = 42,
            IsActive = true
        };

        // Act
        payload.Data = complexData;

        // Assert
        payload.Data.Should().BeEquivalentTo(complexData);
    }

    [Fact]
    public void Signature_ShouldBeSetToValidSignature()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        var signature = "sha256=abc123def456";
        payload.Signature = signature;

        // Assert
        payload.Signature.Should().Be(signature);
    }

    [Fact]
    public void Signature_WithEmptyString_ShouldSetEmptyString()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        payload.Signature = string.Empty;

        // Assert
        payload.Signature.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSerializable()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "TenantCreatedEvent",
            TenantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Data = new { Test = "data" },
            Signature = "test-signature"
        };

        // Act & Assert - This will throw if serialization fails
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        json.Should().NotBeNullOrEmpty();

        // Deserialize back
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<WebhookPayload>(json);
        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(payload.EventId);
        deserialized.EventType.Should().Be(payload.EventType);
        deserialized.TenantId.Should().Be(payload.TenantId);
        deserialized.Timestamp.Should().BeWithin(TimeSpan.FromSeconds(1)).After(payload.Timestamp.AddSeconds(-1));
        deserialized.Signature.Should().Be(payload.Signature);
    }

    [Fact]
    public void JsonPropertyNames_ShouldMatchExpectedNames()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act - Set properties
        payload.EventId = "test-event-id";
        payload.EventType = "test-event-type";
        payload.TenantId = Guid.NewGuid();
        payload.Timestamp = DateTime.UtcNow;
        payload.Data = new { Test = "data" };
        payload.Signature = "test-signature";

        // Serialize to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Assert - Verify JSON property names match expected format
        json.Should().Contain("\"eventId\":");
        json.Should().Contain("\"eventType\":");
        json.Should().Contain("\"tenantId\":");
        json.Should().Contain("\"timestamp\":");
        json.Should().Contain("\"data\":");
        json.Should().Contain("\"signature\":");
    }

    [Fact]
    public void Timestamp_ShouldDefaultToUtcNow()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Assert
        payload.Timestamp.Should().BeWithin(TimeSpan.FromSeconds(2)).After(DateTime.UtcNow.AddSeconds(-2));
    }

    [Fact]
    public void Data_WithNull_ShouldRemainNull()
    {
        // Arrange
        var payload = new WebhookPayload { Data = null };

        // Assert
        payload.Data.Should().BeNull();
    }
}