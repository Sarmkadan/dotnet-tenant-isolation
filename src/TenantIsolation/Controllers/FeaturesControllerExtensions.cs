using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace TenantIsolation.Controllers
{
    /// <summary>
    /// Extension methods for <see cref="FeaturesController"/> that provide strongly-typed access to feature management operations.
    /// </summary>
    public static class FeaturesControllerExtensions
    {
        /// <summary>
        /// Checks if a feature is enabled for the current tenant.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        public static async Task<bool> IsFeatureEnabledAsync(this FeaturesController controller, string featureName)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

            var result = await controller.IsFeatureEnabled(featureName);
            return result is OkObjectResult;
        }

        /// <summary>
        /// Gets the feature configuration as a strongly-typed object.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature.</param>
        /// <returns>The feature configuration or null if not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        public static async Task<FeatureConfiguration?> GetFeatureConfigurationAsync(this FeaturesController controller, string featureName)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

            var result = await controller.GetFeature(featureName);
            return result switch
            {
                OkObjectResult okResult => okResult.Value is FeatureConfiguration config ? config : null,
                _ => null
            };
        }

        /// <summary>
        /// Enables a feature for the current tenant.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to enable.</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        public static async Task<bool> EnableFeatureAsync(this FeaturesController controller, string featureName)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

            var result = await controller.EnableFeature(featureName);
            return result is OkObjectResult;
        }

        /// <summary>
        /// Disables a feature for the current tenant.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to disable.</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        public static async Task<bool> DisableFeatureAsync(this FeaturesController controller, string featureName)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

            var result = await controller.DisableFeature(featureName);
            return result is OkObjectResult;
        }

        /// <summary>
        /// Sets the rollout percentage for a feature.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="percentage">The rollout percentage (0-100).</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="percentage"/> is not between 0 and 100.</exception>
        public static async Task<bool> SetFeatureRolloutPercentageAsync(this FeaturesController controller, string featureName, int percentage)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
            ArgumentOutOfRangeException.ThrowIfLessThan(percentage, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(percentage, 100);

            var request = new SetRolloutRequest { Percentage = percentage };
            var result = await controller.SetRolloutPercentage(featureName, request);
            return result is OkObjectResult;
        }

        /// <summary>
        /// Records feature usage for the current tenant.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="amount">The amount to record (default: 1).</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="featureName"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is less than 1.</exception>
        public static async Task<bool> RecordFeatureUsageAsync(this FeaturesController controller, string featureName, long amount = 1)
        {
            ArgumentNullException.ThrowIfNull(controller);
            ArgumentException.ThrowIfNullOrWhiteSpace(featureName);
            ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);

            var request = new RecordUsageRequest { Amount = amount };
            var result = await controller.RecordUsage(featureName, request);
            return result is OkObjectResult;
        }
    }

    /// <summary>
    /// Represents a feature configuration with its current state.
    /// </summary>
    public sealed class FeatureConfiguration
    {
        /// <summary>
        /// Gets or sets whether the feature is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the rollout percentage (0-100).
        /// </summary>
        public int RolloutPercentage { get; set; }

        /// <summary>
        /// Gets or sets the current usage count.
        /// </summary>
        public long UsageCount { get; set; }

        /// <summary>
        /// Gets or sets the usage limit.
        /// </summary>
        public long UsageLimit { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsed { get; set; }
    }
}
