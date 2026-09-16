using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TenantIsolation.Models
{
    /// <summary>
    /// Extension methods for <see cref="Tenant"/>.
    /// </summary>
    public static class TenantExtensions
    {
        /// <summary>
        /// Determines whether the tenant is considered active.
        /// </summary>
        /// <remarks>
        /// The implementation uses the existing tenant methods that are known to be present:
        /// <see cref="Tenant.IsSubscriptionValid"/> and <see cref="Tenant.IsInTrial"/>.
        /// A tenant is active when its subscription is valid and it is not in a trial period.
        /// </remarks>
        /// <param name="tenant">The tenant instance.</param>
        /// <returns><c>true</c> if the tenant is active; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> is <c>null</c>.</exception>
        public static bool IsActive(this Tenant tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            // Use the existing methods on Tenant to infer activity.
            return tenant.IsSubscriptionValid() && !tenant.IsInTrial();
        }

        /// <summary>
        /// Determines whether the tenant's trial period has expired as of the specified time.
        /// </summary>
        /// <param name="tenant">The tenant instance.</param>
        /// <param name="now">The time against which to evaluate the trial expiration.</param>
        /// <returns>
        /// <c>true</c> if the tenant is in a trial period with an expiration at or before
        /// <paramref name="now"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> is <c>null</c>.</exception>
        public static bool IsTrialExpired(this Tenant tenant, DateTimeOffset now)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            return tenant.IsInTrial() &&
                   tenant.SubscriptionExpiresAt.HasValue &&
                   tenant.SubscriptionExpiresAt.Value <= now.UtcDateTime;
        }

        /// <summary>
        /// Checks whether the tenant has a specific feature enabled.
        /// </summary>
        /// <remarks>
        /// This method looks for a public property named <c>Features</c> on the tenant that
        /// implements <see cref="IEnumerable{T}"/> of <see cref="string"/>. If such a property
        /// exists, the method checks whether the supplied <paramref name="featureName"/> is
        /// present in that collection. If the property does not exist, the method returns
        /// <c>false</c>.
        /// </remarks>
        /// <param name="tenant">The tenant instance.</param>
        /// <param name="featureName">The name of the feature to check.</param>
        /// <returns><c>true</c> if the feature is present; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> is <c>null</c>.</exception>
        public static bool HasFeature(this Tenant tenant, string featureName)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            if (string.IsNullOrWhiteSpace(featureName)) return false;

            // Look for a property called "Features" that returns IEnumerable<string>
            PropertyInfo? prop = tenant.GetType().GetProperty("Features", BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetValue(tenant) is IEnumerable<string> features)
            {
                return features.Contains(featureName, StringComparer.OrdinalIgnoreCase);
            }

            // No suitable property found – assume the feature is not present.
            return false;
        }

        /// <summary>
        /// Gets a display‑friendly name for the tenant.
        /// </summary>
        /// <remarks>
        /// The method first attempts to read a public <c>Name</c> or <c>DisplayName</c> property.
        /// If neither property exists or the value is null/empty, the tenant's type name is
        /// returned as a fallback.
        /// </remarks>
        /// <param name="tenant">The tenant instance.</param>
        /// <returns>A string suitable for display purposes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tenant"/> is <c>null</c>.</exception>
        public static string DisplayName(this Tenant tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            // Prefer a property named "Name", then "DisplayName".
            PropertyInfo? prop = tenant.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)
                               ?? tenant.GetType().GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);

            var value = prop?.GetValue(tenant) as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // Fallback to the type name if no suitable property is found.
            return tenant.GetType().Name;
        }
    }
}
