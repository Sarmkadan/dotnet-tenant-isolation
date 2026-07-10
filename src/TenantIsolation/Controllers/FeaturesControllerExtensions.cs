using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TenantIsolation.Controllers
{
    public static class FeaturesControllerExtensions
    {
        /// <summary>
        /// Checks if a feature is enabled for the current tenant and returns a boolean result.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public static async Task<bool> IsFeatureEnabledAsync(this FeaturesController controller, string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty.", nameof(featureName));
            }

            var result = await controller.IsFeatureEnabled(featureName);
            return result is OkObjectResult okResult && okResult.Value is bool isEnabled && isEnabled;
        }

        /// <summary>
        /// Gets the feature configuration as a strongly-typed object.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature.</param>
        /// <returns>The feature configuration or null if not found.</returns>
        public static async Task<FeatureConfiguration?> GetFeatureConfigurationAsync(this FeaturesController controller, string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty.", nameof(featureName));
            }

            var result = await controller.GetFeature(featureName);
            return result switch
            {
                OkObjectResult okResult when okResult.Value is FeatureConfiguration config => config,
                _ => null
            };
        }

        /// <summary>
        /// Enables a feature for the current tenant with the specified rollout percentage.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to enable.</param>
        /// <param name="percentage">The rollout percentage (0-100).</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static async Task<bool> EnableFeatureWithPercentageAsync(this FeaturesController controller, string featureName, int percentage)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty.", nameof(featureName));
            }

            if (percentage < 0 || percentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be between 0 and 100.");
            }

            var result = await controller.EnableFeature(featureName);
            return result is OkResult;
        }

        /// <summary>
        /// Disables a feature for the current tenant and records the usage.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="featureName">The name of the feature to disable.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static async Task<bool> DisableFeatureWithUsageAsync(this FeaturesController controller, string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty.", nameof(featureName));
            }

            var result = await controller.DisableFeature(featureName);
            return result is OkResult;
        }
    }

    /// <summary>
    /// Represents a feature configuration with its current state.
    /// </summary>
    public class FeatureConfiguration
    {
        public bool IsEnabled { get; set; }
        public int RolloutPercentage { get; set; }
        public long UsageCount { get; set; }
        public long UsageLimit { get; set; }
        public DateTime? LastUsed { get; set; }
    }
}