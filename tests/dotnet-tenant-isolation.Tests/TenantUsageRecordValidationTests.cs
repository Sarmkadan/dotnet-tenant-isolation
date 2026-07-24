#nullable enable

using System;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantUsageRecordValidationTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private const string TestMetricKey = "api_calls";
    private const long TestCurrentValue = 100;
    private const long TestQuotaLimit = 1000;
    private const UsagePeriod TestPeriod = UsagePeriod.Monthly;
    private static readonly DateTime TestPeriodStart = DateTime.UtcNow.AddDays(-1);
    private static readonly DateTime? TestResetAt = DateTime.UtcNow.AddDays(-1).AddHours(1);
    private static readonly DateTime TestCreatedAt = DateTime.UtcNow.AddMinutes(-1);
    private static readonly DateTime TestUpdatedAt = DateTime.UtcNow;

    private static TenantUsageRecord CreateValidRecord()
    {
        return new TenantUsageRecord
        {
            Id = TestId,
            TenantId = TestTenantId,
            MetricKey = TestMetricKey,
            CurrentValue = TestCurrentValue,
            QuotaLimit = TestQuotaLimit,
            Period = TestPeriod,
            PeriodStart = TestPeriodStart,
            ResetAt = null,
            CreatedAt = TestCreatedAt,
            UpdatedAt = TestUpdatedAt
        };
    }

    [Fact]
    public void Validate_WithValidRecord_ReturnsEmptyList()
    {
        // Arrange
        var record = CreateValidRecord();

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithNullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        TenantUsageRecord? record = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record!.Validate());
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsIdError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.Id = Guid.Empty;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("Id must be a non-empty GUID"));
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsTenantIdError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.TenantId = Guid.Empty;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("TenantId must be a non-empty GUID"));
    }

    [Fact]
    public void Validate_WithNullOrWhitespaceMetricKey_ReturnsMetricKeyError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.MetricKey = null!;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("MetricKey must not be null or whitespace"));

        // Arrange - empty string
        record.MetricKey = string.Empty;

        // Act
        errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("MetricKey must not be null or whitespace"));

        // Arrange - whitespace only
        record.MetricKey = "   ";

        // Act
        errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("MetricKey must not be null or whitespace"));
    }

    [Fact]
    public void Validate_WithMetricKeyTooLong_ReturnsMetricKeyLengthError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.MetricKey = new string('a', 101); // 101 characters

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("MetricKey must not exceed 100 characters"));
    }

    [Fact]
    public void Validate_WithNegativeCurrentValue_ReturnsCurrentValueError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.CurrentValue = -1;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("CurrentValue must be non-negative"));
    }

    [Fact]
    public void Validate_WithNegativeQuotaLimit_ReturnsQuotaLimitError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.QuotaLimit = -100;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("QuotaLimit must be non-negative when specified"));
    }

    [Fact]
    public void Validate_WithDefaultPeriodStart_ReturnsPeriodStartError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.PeriodStart = default;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("PeriodStart must be a valid DateTime"));
    }

    [Fact]
    public void Validate_WithPeriodStartMoreThanOneYearInFuture_ReturnsPeriodStartError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.PeriodStart = DateTime.UtcNow.AddYears(2);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("PeriodStart cannot be more than one year in the future"));
    }

    [Fact]
    public void Validate_WithDefaultResetAt_DoesNotReturnError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.ResetAt = default;

        // Act
        var errors = record.Validate();

        // Assert - ResetAt is optional, so null/default is valid
        Assert.DoesNotContain(errors, e => e.Contains("ResetAt"));
    }

    [Fact]
    public void Validate_WithResetAtMoreThanOneYearInFuture_ReturnsResetAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.ResetAt = DateTime.UtcNow.AddYears(2);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("ResetAt cannot be more than one year in the future"));
    }

    [Fact]
    public void Validate_WithResetAtBeforePeriodStart_ReturnsResetAtBeforePeriodStartError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.ResetAt = DateTime.UtcNow.AddDays(-5);
        record.PeriodStart = DateTime.UtcNow.AddDays(-3);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("ResetAt cannot be before PeriodStart"));
    }

    [Fact]
    public void Validate_WithDefaultCreatedAt_ReturnsCreatedAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.CreatedAt = default;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("CreatedAt must be a valid DateTime"));
    }

    [Fact]
    public void Validate_WithCreatedAtMoreThanFiveMinutesInFuture_ReturnsCreatedAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.CreatedAt = DateTime.UtcNow.AddMinutes(10);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("CreatedAt cannot be more than 5 minutes in the future"));
    }

    [Fact]
    public void Validate_WithDefaultUpdatedAt_ReturnsUpdatedAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.UpdatedAt = default;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("UpdatedAt must be a valid DateTime"));
    }

    [Fact]
    public void Validate_WithUpdatedAtMoreThanFiveMinutesInFuture_ReturnsUpdatedAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.UpdatedAt = DateTime.UtcNow.AddMinutes(10);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("UpdatedAt cannot be more than 5 minutes in the future"));
    }

    [Fact]
    public void Validate_WithUpdatedAtBeforeCreatedAt_ReturnsUpdatedAtBeforeCreatedAtError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        record.CreatedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("UpdatedAt cannot be before CreatedAt"));
    }

    [Fact]
    public void IsValid_WithValidRecord_ReturnsTrue()
    {
        // Arrange
        var record = CreateValidRecord();

        // Act
        var isValid = record.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WithInvalidRecord_ReturnsFalse()
    {
        // Arrange
        var record = CreateValidRecord();
        record.Id = Guid.Empty;

        // Act
        var isValid = record.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WithNullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        TenantUsageRecord? record = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record!.IsValid());
    }

    [Fact]
    public void EnsureValid_WithValidRecord_DoesNotThrow()
    {
        // Arrange
        var record = CreateValidRecord();

        // Act
        var exception = Record.Exception(() => record.EnsureValid());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_WithInvalidRecord_ThrowsArgumentException()
    {
        // Arrange
        var record = CreateValidRecord();
        record.Id = Guid.Empty;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => record.EnsureValid());
        Assert.Contains("Id must be a non-empty GUID", exception.Message);
    }

    [Fact]
    public void EnsureValid_WithNullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        TenantUsageRecord? record = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => record!.EnsureValid());
    }

    [Fact]
    public void Validate_WithZeroQuotaLimit_DoesNotReturnError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.QuotaLimit = 0;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.DoesNotContain(errors, e => e.Contains("QuotaLimit"));
    }

    [Fact]
    public void Validate_WithNullQuotaLimit_DoesNotReturnError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.QuotaLimit = null;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.DoesNotContain(errors, e => e.Contains("QuotaLimit"));
    }

    [Fact]
    public void Validate_WithUnlimitedQuota_DoesNotReturnError()
    {
        // Arrange
        var record = CreateValidRecord();
        record.QuotaLimit = null;
        record.CurrentValue = 1000000;

        // Act
        var errors = record.Validate();

        // Assert
        Assert.Empty(errors);
    }
}