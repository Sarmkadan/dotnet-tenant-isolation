#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace TenantIsolation.Integration;

/// <summary>
/// Provides validation helpers for <see cref="ApiCallResult{T}"/>
/// </summary>
public static class ApiCallResultValidation
{
    /// <summary>
    /// Validates an <see cref="ApiCallResult{T}"/> instance and returns a list of validation problems.
    /// </summary>
    /// <typeparam name="T">The type of data in the API call result</typeparam>
    /// <param name="value">The API call result to validate</param>
    /// <returns>A read-only list of human-readable validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate<T>(this ApiCallResult<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Duration is positive
        if (value.Duration <= TimeSpan.Zero)
        {
            problems.Add("Duration must be a positive time span");
        }

        // Validate HttpStatusCode if present and IsSuccess is true
        if (value.IsSuccess && value.HttpStatusCode.HasValue)
        {
            var statusCode = value.HttpStatusCode.Value;
            if (statusCode < 100 || statusCode > 599)
            {
                problems.Add("HttpStatusCode must be a valid HTTP status code (100-599) when IsSuccess is true");
            }
        }

        // Validate ErrorMessage is not null/empty when IsSuccess is false
        if (!value.IsSuccess && !string.IsNullOrEmpty(value.ErrorMessage))
        {
            problems.Add("ErrorMessage must be null or empty when IsSuccess is true");
        }

        // Validate that if IsSuccess is false, ErrorMessage should not be null/empty
        if (!value.IsSuccess && string.IsNullOrEmpty(value.ErrorMessage))
        {
            problems.Add("ErrorMessage must be provided when IsSuccess is false");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ApiCallResult{T}"/> is valid.
    /// </summary>
    /// <typeparam name="T">The type of data in the API call result</typeparam>
    /// <param name="value">The API call result to check</param>
    /// <returns>true if the result is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid<T>(this ApiCallResult<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ApiCallResult{T}"/> is valid, throwing an exception if it is not.
    /// </summary>
    /// <typeparam name="T">The type of data in the API call result</typeparam>
    /// <param name="value">The API call result to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid, containing the validation problems</exception>
    public static void EnsureValid<T>(this ApiCallResult<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ApiCallResult is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
