namespace TenantIsolation.Models;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

/// <summary>
/// Provides extension methods for <see cref="TenantConfiguration"/>.
/// </summary>
public static class TenantConfigurationExtensions
{
    /// <summary>
    /// Determines if a <see cref="TenantConfiguration"/> is valid for a given <typeparamref name="T"/> value type.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="configuration">The tenant configuration.</param>
    /// <returns>true if the configuration is valid for the given value type; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    public static bool IsValidForValueType<T>(this TenantConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return string.Equals(configuration.ValueType, typeof(T).Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to get the value of a <see cref="TenantConfiguration"/> as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="configuration">The tenant configuration.</param>
    /// <param name="value">When this method returns, contains the value if successful; otherwise, the default value for type <typeparamref name="T"/>.</param>
    /// <returns>true if the value was successfully retrieved; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    public static bool TryGetValueAs<T>(this TenantConfiguration configuration, [NotNullWhen(true)] out T? value)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        try
        {
            value = configuration.GetValueAs<T>();
            return true;
        }
        catch (FormatException) // Value is not in correct format for the type
        {
            value = default;
            return false;
        }
        catch (OverflowException) // Value represents a number that is out of range
        {
            value = default;
            return false;
        }
        catch (InvalidCastException) // Conversion not supported
        {
            value = default;
            return false;
        }
        catch (JsonException) // JSON deserialization failed
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Updates the value of a <see cref="TenantConfiguration"/> and sets the <see cref="TenantConfiguration.ModifiedAt"/> property.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="configuration">The tenant configuration.</param>
    /// <param name="newValue">The new value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    public static void UpdateValue<T>(this TenantConfiguration configuration, T newValue)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.SetValue(newValue);
        configuration.ModifiedAt = DateTime.UtcNow;
    }
}