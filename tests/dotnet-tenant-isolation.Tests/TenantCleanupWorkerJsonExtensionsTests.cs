using FluentAssertions;
using Xunit;

namespace TenantIsolation.BackgroundTasks;

/// <summary>
/// Contains tests for the JSON serialization and deserialization extension methods of the <see cref="TenantCleanupWorker"/> class.
/// </summary>
public class TenantCleanupWorkerJsonExtensionsTests
{
    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker)"/> method returns a non-empty JSON string containing the checkInterval and retentionPeriod properties when called on a valid worker instance.
    /// </summary>
    [Fact]
    public void ToJson_WithValidWorker_ReturnsNonEmptyJsonString()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval = TimeSpan.FromHours(2);
        worker.RetentionPeriod = TimeSpan.FromDays(7);

        // Act
        var json = worker.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("checkInterval");
        json.Should().Contain("retentionPeriod");
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker,bool)"/> method returns formatted JSON (with newlines and indentation) when the indented parameter is set to true.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);

        // Act
        var json = worker.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\n"); // Should have newlines for formatting
        json.Should().Contain("  "); // Should have indentation
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker,bool)"/> method returns compact JSON (without newlines) when the indented parameter is set to false.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);

        // Act
        var json = worker.ToJson(indented: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n"); // Should not have newlines
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker)"/> method throws an <see cref="ArgumentNullException"/> when the worker parameter is null.
    /// </summary>
    [Fact]
    public void ToJson_WithNullWorker_ThrowsArgumentNullException()
    {
        // Arrange
        TenantCleanupWorker worker = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => worker.ToJson());
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> method correctly deserializes a valid JSON string into a <see cref="TenantCleanupWorker"/> instance with the expected checkInterval and retentionPeriod values.
    /// </summary>
    [Fact]
    public void FromJson_WithValidJson_ReturnsWorkerInstance()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval = TimeSpan.FromHours(3);
        worker.RetentionPeriod = TimeSpan.FromDays(14);

        var json = worker.ToJson();

        // Act
        var result = TenantCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        result.Should().NotBeNull();
        result!.CheckInterval.Should().Be(TimeSpan.FromHours(3));
        result.RetentionPeriod.Should().Be(TimeSpan.FromDays(14));
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> method returns null when the JSON string is empty.
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var result = TenantCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> method returns null when the JSON string contains only whitespace.
    /// </summary>
    [Fact]
    public void FromJson_WithWhitespaceString_ReturnsNull()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act
        var result = TenantCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> method throws an <see cref="ArgumentNullException"/> when the JSON string is null.
    /// </summary>
    [Fact]
    public void FromJson_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TenantCleanupWorkerJsonExtensions.FromJson(json));
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> method throws a <see cref="System.Text.Json.JsonException"/> when the JSON string is invalid.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "invalid json {{{";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => TenantCleanupWorkerJsonExtensions.FromJson(json));
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.TryFromJson(string,TenantCleanupWorker@)"/> method returns true and populates the worker instance when the JSON string is valid.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndWorkerInstance()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval = TimeSpan.FromHours(5);
        worker.RetentionPeriod = TimeSpan.FromDays(21);

        var json = worker.ToJson();

        // Act
        var result = TenantCleanupWorkerJsonExtensions.TryFromJson(json, out var workerInstance);

        // Assert
        result.Should().BeTrue();
        workerInstance.Should().NotBeNull();
        workerInstance!.CheckInterval.Should().Be(TimeSpan.FromHours(5));
        workerInstance.RetentionPeriod.Should().Be(TimeSpan.FromDays(21));
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.TryFromJson(string,TenantCleanupWorker@)"/> method returns false and null when the JSON string is empty.
    /// </summary>
    [Fact]
    public void TryFromJson_WithEmptyString_ReturnsFalseAndNull()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var result = TenantCleanupWorkerJsonExtensions.TryFromJson(json, out var workerInstance);

        // Assert
        result.Should().BeFalse();
        workerInstance.Should().BeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.TryFromJson(string,TenantCleanupWorker@)"/> method returns false and null when the JSON string contains only whitespace.
    /// </summary>
    [Fact]
    public void TryFromJson_WithWhitespaceString_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act
        var result = TenantCleanupWorkerJsonExtensions.TryFromJson(json, out var workerInstance);

        // Assert
        result.Should().BeFalse();
        workerInstance.Should().BeNull();
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.TryFromJson(string,TenantCleanupWorker@)"/> method throws an <see cref="ArgumentNullException"/> when the JSON string is null.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TenantCleanupWorkerJsonExtensions.TryFromJson(json, out _));
    }

    /// <summary>
    /// Tests that the <see cref="TenantCleanupWorkerJsonExtensions.TryFromJson(string,TenantCleanupWorker@)"/> method returns false and null when the JSON string is invalid.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "invalid json {{{";

        // Act
        var result = TenantCleanupWorkerJsonExtensions.TryFromJson(json, out var workerInstance);

        // Assert
        result.Should().BeFalse();
        workerInstance.Should().BeNull();
    }

    /// <summary>
    /// Tests that the round-trip serialization (serialize then deserialize) of a <see cref="TenantCleanupWorker"/> instance using <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker)"/> and <see cref="TenantCleanupWorkerJsonExtensions.FromJson(string)"/> preserves all property values.
    /// </summary>
    [Fact]
    public void RoundtripSerialization_PreservesAllProperties()
    {
        // Arrange
        var original = new TenantCleanupWorker(null!, null!);
        original.CheckInterval = TimeSpan.FromHours(12);
        original.RetentionPeriod = TimeSpan.FromDays(30);

        // Act
        var json = original.ToJson();
        var deserialized = TenantCleanupWorkerJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CheckInterval.Should().Be(original.CheckInterval);
        deserialized.RetentionPeriod.Should().Be(original.RetentionPeriod);
    }

    /// <summary>
    /// Tests that the round-trip serialization (serialize then deserialize using TryFromJson) of a <see cref="TenantCleanupWorker"/> instance preserves all property values.
    /// </summary>
    [Fact]
    public void RoundtripSerialization_WithTryFromJson_PreservesAllProperties()
    {
        // Arrange
        var original = new TenantCleanupWorker(null!, null!);
        original.CheckInterval = TimeSpan.FromMinutes(45);
        original.RetentionPeriod = TimeSpan.FromDays(45);

        var json = original.ToJson();

        // Act
        var result = TenantCleanupWorkerJsonExtensions.TryFromJson(json, out var deserialized);

        // Assert
        result.Should().BeTrue();
        deserialized.Should().NotBeNull();
        deserialized!.CheckInterval.Should().Be(original.CheckInterval);
        deserialized.RetentionPeriod.Should().Be(original.RetentionPeriod);
    }

    /// <summary>
    /// Tests that the JSON produced by the <see cref="TenantCleanupWorkerJsonExtensions.ToJson(TenantCleanupWorker)"/> method uses camelCase naming for property names (as opposed to PascalCase).
    /// </summary>
    [Fact]
    public void JsonUsesCamelCaseNamingPolicy()
    {
        // Arrange
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval = TimeSpan.FromHours(1);
        worker.RetentionPeriod = TimeSpan.FromDays(1);

        var json = worker.ToJson();

        // Act & Assert
        json.Should().Contain("checkInterval");
        json.Should().Contain("retentionPeriod");
        json.Should().NotContain("CheckInterval"); // PascalCase should not be present
        json.Should().NotContain("RetentionPeriod"); // PascalCase should not be present
    }
}