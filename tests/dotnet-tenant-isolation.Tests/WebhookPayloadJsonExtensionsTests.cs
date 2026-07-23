using System.Text.Json;
using Xunit;

namespace TenantIsolation.Integration;

public class WebhookPayloadJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange
        var payload = new WebhookPayload();

        // Act
        var json = WebhookPayloadJsonExtensions.ToJson(payload);

        // Assert
        Assert.NotNull(json);
    }

    [Fact]
    public void ToJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => WebhookPayloadJsonExtensions.ToJson(null));
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsWebhookPayload()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new WebhookPayload());

        // Act
        var payload = WebhookPayloadJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(payload);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentException()
    {
        // Act and Assert
        Assert.Throws<ArgumentException>(() => WebhookPayloadJsonExtensions.FromJson(null));
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        // Act
        var payload = WebhookPayloadJsonExtensions.FromJson("Invalid json");

        // Assert
        Assert.Null(payload);
    }

    [Fact]
    public void TryFromJson_HappyPath_ReturnsTrueAndWebhookPayload()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new WebhookPayload());

        // Act
        var success = WebhookPayloadJsonExtensions.TryFromJson(json, out var payload);

        // Assert
        Assert.True(success);
        Assert.NotNull(payload);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentException()
    {
        // Act and Assert
        Assert.Throws<ArgumentException>(() => WebhookPayloadJsonExtensions.TryFromJson(null, out _));
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Act
        var success = WebhookPayloadJsonExtensions.TryFromJson("Invalid json", out var payload);

        // Assert
        Assert.False(success);
        Assert.Null(payload);
    }
}
