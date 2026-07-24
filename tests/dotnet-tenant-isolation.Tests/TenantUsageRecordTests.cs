#nullable enable

using System;
using FluentAssertions;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public sealed class TenantUsageRecordTests
{
    [Fact]
    public void Constructor_Default_CreatesRecordWithDefaults()
    {
        // Arrange & Act
        var record = new TenantUsageRecord();

        // Assert
        record.Id.Should().NotBe(Guid.Empty);
        record.TenantId.Should().Be(Guid.Empty);
        record.Tenant.Should().BeNull();
        record.MetricKey.Should().BeNull();
        record.CurrentValue.Should().Be(0);
        record.QuotaLimit.Should().BeNull();
        record.Period.Should().Be(UsagePeriod.Monthly);
        record.PeriodStart.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        record.ResetAt.Should().BeNull();
        record.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        record.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var resetAt = now.AddDays(-1);
        var record = new TenantUsageRecord
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            MetricKey = "api_calls",
            CurrentValue = 150,
            QuotaLimit = 1000,
            Period = UsagePeriod.Daily,
            PeriodStart = now,
            ResetAt = resetAt,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-1)
        };

        // Assert
        record.Id.Should().NotBe(Guid.Empty);
        record.TenantId.Should().NotBe(Guid.Empty);
        record.MetricKey.Should().Be("api_calls");
        record.CurrentValue.Should().Be(150);
        record.QuotaLimit.Should().Be(1000);
        record.Period.Should().Be(UsagePeriod.Daily);
        record.PeriodStart.Should().Be(now);
        record.ResetAt.Should().Be(resetAt);
    }

    [Theory]
    [InlineData(50, 100L, 50.0)]
    [InlineData(150, 100L, 100.0)]
    [InlineData(0, 100L, 0.0)]
    [InlineData(7500, 10000L, 75.0)]
    public void UsagePercentage_CalculatesCorrectPercentage(long current, long? limit, double expected)
    {
        // Arrange
        var record = new TenantUsageRecord { CurrentValue = current, QuotaLimit = limit };

        // Act & Assert
        record.UsagePercentage.Should().Be(expected);
    }

    [Fact]
    public void UsagePercentage_WithNullLimit_ReturnsZero()
    {
        // Arrange
        var record = new TenantUsageRecord { CurrentValue = 50, QuotaLimit = null };

        // Act & Assert
        record.UsagePercentage.Should().Be(0.0);
    }

    [Theory]
    [InlineData(50, 100L, false)]
    [InlineData(100, 100L, true)]
    [InlineData(150, 100L, true)]
    [InlineData(50, null, false)]
    [InlineData(50, 0L, true)]
    public void IsQuotaExceeded_ReturnsCorrectResult(long current, long? limit, bool expected)
    {
        // Arrange
        var record = new TenantUsageRecord { CurrentValue = current, QuotaLimit = limit };

        // Act & Assert
        record.IsQuotaExceeded.Should().Be(expected);
    }

    [Theory]
    [InlineData(50, 1000L, 80, false)]
    [InlineData(800, 1000L, 80, true)]
    [InlineData(850, 1000L, 80, true)]
    [InlineData(50, null, 80, false)]
    [InlineData(60, 100L, 60, true)]
    public void IsApproachingLimit_ReturnsCorrectResult(long current, long? limit, int threshold, bool expected)
    {
        // Arrange
        var record = new TenantUsageRecord { CurrentValue = current, QuotaLimit = limit };

        // Act & Assert
        record.IsApproachingLimit(threshold).Should().Be(expected);
    }

    [Fact]
    public void IsApproachingLimit_WithDefaultThreshold_ReturnsTrueAt80Percent()
    {
        // Arrange
        var record = new TenantUsageRecord { CurrentValue = 80, QuotaLimit = 100 };

        // Act & Assert
        record.IsApproachingLimit().Should().BeTrue();
    }
}

public sealed class QuotaCheckResultTests
{
    [Theory]
    [InlineData("storage_gb", 50L, 100L, true, false, 50.0)]
    [InlineData("api_calls", 100L, null, true, false, 0.0)]
    [InlineData("bandwidth_mb", 7500L, 10000L, true, false, 75.0)]
    public void Allow_CreatesCorrectResult(string metricKey, long current, long? limit, bool isAllowed, bool isExceeded, double percentage)
    {
        // Act
        var result = QuotaCheckResult.Allow(metricKey, current, limit);

        // Assert
        result.IsAllowed.Should().Be(isAllowed);
        result.IsExceeded.Should().Be(isExceeded);
        result.CurrentUsage.Should().Be(current);
        result.QuotaLimit.Should().Be(limit);
        result.MetricKey.Should().Be(metricKey);
        result.UsagePercentage.Should().Be(percentage);
        result.ViolationMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("api_calls", 150L, 100L, false, true, 100.0)]
    [InlineData("storage_gb", 10000L, 5000L, false, true, 100.0)]
    [InlineData("api_calls", 1L, 0L, false, true, 100.0)]
    public void Deny_CreatesCorrectResult(string metricKey, long current, long limit, bool isAllowed, bool isExceeded, double percentage)
    {
        // Act
        var result = QuotaCheckResult.Deny(metricKey, current, limit);

        // Assert
        result.IsAllowed.Should().Be(isAllowed);
        result.IsExceeded.Should().Be(isExceeded);
        result.CurrentUsage.Should().Be(current);
        result.QuotaLimit.Should().Be(limit);
        result.MetricKey.Should().Be(metricKey);
        result.UsagePercentage.Should().Be(percentage);
        result.ViolationMessage.Should().NotBeNull();
        result.ViolationMessage.Should().Contain(metricKey);
        result.ViolationMessage.Should().Contain(limit.ToString());
        result.ViolationMessage.Should().Contain(current.ToString());
    }
}