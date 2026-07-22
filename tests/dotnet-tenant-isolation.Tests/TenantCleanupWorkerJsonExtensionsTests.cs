using FluentAssertions;
using Xunit;

namespace TenantIsolation.BackgroundTasks;

public class TenantCleanupWorkerJsonExtensionsTests
{
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

    [Fact]
    public void ToJson_WithNullWorker_ThrowsArgumentNullException()
    {
        // Arrange
        TenantCleanupWorker worker = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => worker.ToJson());
    }

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

    [Fact]
    public void FromJson_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TenantCleanupWorkerJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "invalid json {{{";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => TenantCleanupWorkerJsonExtensions.FromJson(json));
    }

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

    [Fact]
    public void TryFromJson_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TenantCleanupWorkerJsonExtensions.TryFromJson(json, out _));
    }

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