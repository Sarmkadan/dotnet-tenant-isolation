using System;
using TenantIsolation.Models;
using Xunit;

namespace dotnet_tenant_isolation.Tests
{
    public class TenantFeatureTests
    {
        private TenantFeature CreateDefaultFeature()
        {
            return new TenantFeature
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                FeatureKey = "test-feature",
                IsEnabled = true,
                RolloutPercentage = 100,
                AvailabilityLevel = "GA",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void IsAvailable_ReturnsTrue_WhenEnabledAndNoRestrictions()
        {
            var feature = CreateDefaultFeature();

            Assert.True(feature.IsAvailable());
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenDisabled()
        {
            var feature = CreateDefaultFeature();
            feature.IsEnabled = false;

            Assert.False(feature.IsAvailable());
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenAvailableFromInFuture()
        {
            var feature = CreateDefaultFeature();
            feature.AvailableFrom = DateTime.UtcNow.AddHours(1);

            Assert.False(feature.IsAvailable());
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenDeprecatedInPast()
        {
            var feature = CreateDefaultFeature();
            feature.DeprecatedAt = DateTime.UtcNow.AddHours(-1);

            Assert.False(feature.IsAvailable());
        }

        [Fact]
        public void IsAvailable_RespectsRolloutPercentage()
        {
            var feature = CreateDefaultFeature();
            feature.RolloutPercentage = 0; // 0% rollout should always be unavailable

            Assert.False(feature.IsAvailable());
        }

        [Fact]
        public void IsUsageLimitExceeded_ReturnsTrue_WhenLimitReached()
        {
            var feature = CreateDefaultFeature();
            feature.UsageLimit = 10;
            feature.CurrentUsage = 10;

            Assert.True(feature.IsUsageLimitExceeded());
        }

        [Fact]
        public void CanUseFeature_ReturnsFalse_WithCorrectError_WhenDisabled()
        {
            var feature = CreateDefaultFeature();
            feature.IsEnabled = false;

            var result = feature.CanUseFeature(out string? error);

            Assert.False(result);
            Assert.Equal("This feature is not enabled", error);
        }

        [Fact]
        public void CanUseFeature_ReturnsFalse_WithCorrectError_WhenUsageLimitExceeded()
        {
            var feature = CreateDefaultFeature();
            feature.UsageLimit = 5;
            feature.CurrentUsage = 5;

            var result = feature.CanUseFeature(out string? error);

            Assert.False(result);
            Assert.Equal($"Usage limit of {feature.UsageLimit} has been reached", error);
        }

        [Fact]
        public void RecordUsage_IncrementsCurrentUsage_AndUpdatesTimestamp()
        {
            var feature = CreateDefaultFeature();
            var before = feature.UpdatedAt;

            feature.RecordUsage(3);

            Assert.Equal(3, feature.CurrentUsage);
            Assert.True(feature.UpdatedAt > before);
        }

        [Fact]
        public void ResetUsage_SetsCurrentUsageToZero_AndUpdatesTimestamp()
        {
            var feature = CreateDefaultFeature();
            feature.CurrentUsage = 42;
            var before = feature.UpdatedAt;

            feature.ResetUsage();

            Assert.Equal(0, feature.CurrentUsage);
            Assert.True(feature.UpdatedAt > before);
        }

        [Theory]
        [InlineData(null, true, "Active")]
        [InlineData("2025-01-01", true, "Pending")]
        [InlineData(null, false, "Disabled")]
        [InlineData("2020-01-01", true, "Deprecated")]
        [InlineData(null, true, "Beta (30%)", 30)]
        public void GetStatus_ReturnsExpectedString(
            string? dateString,
            bool isEnabled,
            string expectedStatus,
            int rollout = 100)
        {
            var feature = CreateDefaultFeature();
            feature.IsEnabled = isEnabled;
            feature.RolloutPercentage = rollout;

            if (dateString != null)
            {
                var date = DateTime.Parse(dateString);
                if (expectedStatus == "Pending")
                    feature.AvailableFrom = date;
                else if (expectedStatus == "Deprecated")
                    feature.DeprecatedAt = date;
            }

            var status = feature.GetStatus();

            Assert.Equal(expectedStatus, status);
        }
    }
}
