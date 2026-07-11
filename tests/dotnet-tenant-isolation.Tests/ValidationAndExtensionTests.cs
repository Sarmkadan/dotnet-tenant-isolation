#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Data;
using TenantIsolation.Exceptions;
using TenantIsolation.Models;
using TenantIsolation.Services;
using TenantIsolation.Utilities;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Provides unit tests for the <see cref="ValidationUtility"/> class methods.
/// Tests various validation scenarios including email validation, slug validation,
/// and range/positive value requirements.
/// </summary>
public class ValidationUtilityTests
{
	/// <summary>
	/// Tests <see cref="ValidationUtility.IsValidEmail(string)"/> with various email inputs.
	/// Validates that the method correctly identifies valid and invalid email formats.
	/// </summary>
	/// <param name="email">The email address to validate.</param>
	/// <param name="expected">The expected result (true for valid, false for invalid).</param>
	[Theory]
	[InlineData("user@example.com", true)]
	[InlineData("admin@tenant.io", true)]
	[InlineData("not-an-email", false)]
	[InlineData("@missing-local.com", false)]
	[InlineData("", false)]
	public void IsValidEmail_WithVariousInputs_ReturnsExpectedResult(string email, bool expected)
	{
		// Act & Assert
		ValidationUtility.IsValidEmail(email).Should().Be(expected);
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.IsValidSlug(string)"/> with various slug inputs.
	/// Validates that the method correctly identifies valid and invalid slug formats.
	/// Valid slugs must be lowercase alphanumeric with hyphens, 3-50 characters.
	/// </summary>
	/// <param name="slug">The slug to validate.</param>
	/// <param name="expected">The expected result (true for valid, false for invalid).</param>
	[Theory]
	[InlineData("my-tenant", true)]
	[InlineData("acme123", true)]
	[InlineData("abc", true)]
	[InlineData("UPPERCASE", false)]
	[InlineData("ab", false)]
	[InlineData("slug with spaces", false)]
	[InlineData("has_underscore", false)]
	public void IsValidSlug_WithVariousInputs_ReturnsExpectedResult(string slug, bool expected)
	{
		// Act & Assert
		ValidationUtility.IsValidSlug(slug).Should().Be(expected);
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequireNotEmpty(string, string)"/> when null input is provided.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/> mentioning the field name.
	/// </summary>
	[Fact]
	public void RequireNotEmpty_WhenNull_ThrowsTenantIsolationExceptionMentioningFieldName()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequireNotEmpty(null, "TenantName");

		// Assert
		act.Should().Throw<TenantIsolationException>()
			.WithMessage("*TenantName*");
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequireNotEmpty(string, string)"/> when whitespace-only input is provided.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/>.
	/// </summary>
	[Fact]
	public void RequireNotEmpty_WhenWhitespaceOnly_ThrowsTenantIsolationException()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequireNotEmpty(" ", "TenantName");

		// Assert
		act.Should().Throw<TenantIsolationException>();
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequirePositive(int, string)"/> when zero value is provided.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/> with message mentioning "greater than zero".
	/// </summary>
	[Fact]
	public void RequirePositive_WhenZero_ThrowsTenantIsolationException()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequirePositive(0, "MaxUsers");

		// Assert
		act.Should().Throw<TenantIsolationException>()
			.WithMessage("*greater than zero*");
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequirePositive(int, string)"/> when positive value is provided.
	/// Verifies that the method does not throw any exception.
	/// </summary>
	[Fact]
	public void RequirePositive_WhenPositiveValue_DoesNotThrow()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequirePositive(1, "MaxUsers");

		// Assert
		act.Should().NotThrow();
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequireRange(int, int, int, string)"/> when value is below minimum.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/> with message mentioning "between".
	/// </summary>
	[Fact]
	public void RequireRange_WhenValueBelowMinimum_ThrowsTenantIsolationException()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequireRange(0, 1, 100, "Percentage");

		// Assert
		act.Should().Throw<TenantIsolationException>()
			.WithMessage("*between*");
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequireRange(int, int, int, string)"/> when value is above maximum.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/>.
	/// </summary>
	[Fact]
	public void RequireRange_WhenValueAboveMaximum_ThrowsTenantIsolationException()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequireRange(101, 0, 100, "Percentage");

		// Assert
		act.Should().Throw<TenantIsolationException>();
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.RequireValidSlug(string)"/> when invalid format is provided.
	/// Verifies that the method throws a <see cref="TenantIsolationException"/> with helpful message about invalid slug.
	/// </summary>
	[Fact]
	public void RequireValidSlug_WhenInvalidFormat_ThrowsExceptionWithHelpfulMessage()
	{
		// Arrange & Act
		var act = () => ValidationUtility.RequireValidSlug("INVALID SLUG!");

		// Assert
		act.Should().Throw<TenantIsolationException>()
			.WithMessage("*not a valid slug*");
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.IsValidGuid(string)"/> with a well-formed GUID string.
	/// Verifies that the method returns true for valid GUID format.
	/// </summary>
	[Fact]
	public void IsValidGuid_WithWellFormedGuid_ReturnsTrue()
	{
		// Arrange
		var guid = Guid.NewGuid().ToString();

		// Act & Assert
		ValidationUtility.IsValidGuid(guid).Should().BeTrue();
	}

	/// <summary>
	/// Tests <see cref="ValidationUtility.IsValidGuid(string)"/> with a malformed string.
	/// Verifies that the method returns false for invalid GUID format.
	/// </summary>
	[Fact]
	public void IsValidGuid_WithMalformedString_ReturnsFalse()
	{
		// Act & Assert
		ValidationUtility.IsValidGuid("not-a-guid").Should().BeFalse();
	}
}

public class StringExtensionTests
{
	/// <summary>
	/// Tests <see cref="StringExtensions.ToSlug()"/> with a string containing spaces and uppercase letters.
	/// Verifies that the method converts to lowercase and replaces spaces with hyphens.
	/// </summary>
	[Fact]
	public void ToSlug_WithSpacesAndUppercase_ReturnsLowercaseHyphenated()
	{
		// Act
		var result = "Hello World".ToSlug();

		// Assert
		result.Should().Be("hello-world");
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.ToSlug()"/> with an empty string.
	/// Verifies that the method returns an empty string.
	/// </summary>
	[Fact]
	public void ToSlug_WithEmptyString_ReturnsEmptyString()
	{
		// Act
		var result = "".ToSlug();

		// Assert
		result.Should().BeEmpty();
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.ToSlug()"/> with a string containing special characters.
	/// Verifies that the method removes non-alphanumeric characters.
	/// </summary>
	[Fact]
	public void ToSlug_WithSpecialCharacters_RemovesNonAlphanumeric()
	{
		// Act
		var result = "Acme & Co.".ToSlug();

		// Assert
		result.Should().NotContain("&").And.NotContain(".");
		result.Should().MatchRegex(@"^[a-z0-9\-]+$");
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.Truncate(int)"/> when string is longer than max length.
	/// Verifies that the method truncates and adds ellipsis.
	/// </summary>
	[Fact]
	public void Truncate_WhenLongerThanMaxLength_TruncatesAndAddsEllipsis()
	{
		// Arrange
		const string input = "This is a long string that exceeds the limit";

		// Act
		var result = input.Truncate(10);

		// Assert
		result.Should().HaveLength(10);
		result.Should().EndWith("...");
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.Truncate(int)"/> when string is shorter than max length.
	/// Verifies that the method returns the original string unchanged.
	/// </summary>
	[Fact]
	public void Truncate_WhenShorterThanMaxLength_ReturnsOriginalString()
	{
		// Arrange
		const string input = "Short";

		// Act
		var result = input.Truncate(100);

		// Assert
		result.Should().Be(input);
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.MaskSensitiveData(int)"/> with a sensitive string.
	/// Verifies that the method masks all characters after the visible count.
	/// </summary>
	[Fact]
	public void MaskSensitiveData_MasksAllCharactersAfterVisibleCount()
	{
		// Arrange
		const string input = "secret-password";

		// Act
		var result = input.MaskSensitiveData(3);

		// Assert
		result.Should().StartWith("sec");
		result.Should().HaveLength(input.Length);
		result[3..].Should().MatchRegex(@"^\*+$");
	}

	/// <summary>
	/// Tests the <see cref="StringExtensions.IsValidEmail()"/> extension method with a valid email address.
	/// Verifies that the method returns true for valid email format.
	/// </summary>
	[Fact]
	public void IsValidEmail_ExtensionMethod_WithValidAddress_ReturnsTrue()
	{
		// Act & Assert
		"admin@company.com".IsValidEmail().Should().BeTrue();
	}

	/// <summary>
	/// Tests the <see cref="StringExtensions.IsValidUrl()"/> extension method with an HTTPS URL.
	/// Verifies that the method returns true for valid HTTPS URLs.
	/// </summary>
	[Fact]
	public void IsValidUrl_WithHttpsScheme_ReturnsTrue()
	{
		// Act & Assert
		"https://api.example.com/v1".IsValidUrl().Should().BeTrue();
	}

	/// <summary>
	/// Tests the <see cref="StringExtensions.IsValidUrl()"/> extension method with an FTP URL.
	/// Verifies that the method returns false since only HTTP/HTTPS schemes are accepted.
	/// </summary>
	[Fact]
	public void IsValidUrl_WithFtpScheme_ReturnsFalse()
	{
		// Act & Assert — only http/https are accepted
		"ftp://files.example.com".IsValidUrl().Should().BeFalse();
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.GetDeterministicHashCode()"/> with the same input across multiple calls.
	/// Verifies that the method returns the same hash code for identical inputs.
	/// </summary>
	[Fact]
	public void GetDeterministicHashCode_SameInputAcrossCalls_ReturnsSameHash()
	{
		// Arrange
		const string input = "tenant-identifier";

		// Act
		var first = input.GetDeterministicHashCode();
		var second = input.GetDeterministicHashCode();

		// Assert
		first.Should().Be(second);
	}

	/// <summary>
	/// Tests <see cref="StringExtensions.SafeSubstring(int, int)"/> when start index exceeds string length.
	/// Verifies that the method returns an empty string.
	/// </summary>
	[Fact]
	public void SafeSubstring_WhenStartIndexExceedsLength_ReturnsEmpty()
	{
		// Act
		var result = "short".SafeSubstring(100, 5);

		// Assert
		result.Should().BeEmpty();
	}
}

public class TenantFeatureServiceCacheTests
{
	/// <summary>
	/// Tests <see cref="TenantFeatureService.GetFeatureAsync(Guid, string)"/> when cache hit occurs.
	/// Verifies that the method returns the cached feature without querying the database.
	/// </summary>
	[Fact]
	public async Task GetFeatureAsync_WhenCacheHit_ReturnsCachedFeatureWithoutQueryingDatabase()
	{
		// Arrange
		var tenantId = Guid.NewGuid();
		const string featureKey = "api-access";

		var cachedFeature = new TenantFeature
		{
			Id = Guid.NewGuid(),
			TenantId = tenantId,
			FeatureKey = featureKey,
			IsEnabled = true,
			RolloutPercentage = 100
		};

		var mockCache = new Mock<IMemoryCache>();
		var mockLogger = new Mock<ILogger<TenantFeatureService>>();

		// Moq: set up cache to report a hit and return the pre-populated feature
		object? cachedValue = cachedFeature;
		mockCache
			.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
			.Returns(true);

		var dbOptions = new DbContextOptionsBuilder<TenantDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		await using var context = new TenantDbContext(dbOptions);

		var service = new TenantFeatureService(context, mockCache.Object, mockLogger.Object);

		// Act
		var result = await service.GetFeatureAsync(tenantId, featureKey);

		// Assert
		result.Should().NotBeNull();
		result!.FeatureKey.Should().Be(featureKey);
		result.TenantId.Should().Be(tenantId);
		result.IsEnabled.Should().BeTrue();

		// Cache was consulted exactly once; the database was never reached
		mockCache.Verify(
			c => c.TryGetValue(It.IsAny<object>(), out cachedValue),
			Times.Once);
	}

	/// <summary>
	/// Tests <see cref="TenantFeatureService.GetFeatureAsync(Guid, string)"/> when cache miss occurs and feature exists in database.
	/// Verifies that the method returns the feature from the database and caches it.
	/// </summary>
	[Fact]
	public async Task GetFeatureAsync_WhenCacheMissAndFeatureExists_ReturnsFeatureFromDatabase()
	{
		// Arrange
		var tenantId = Guid.NewGuid();
		const string featureKey = "advanced-security";

		var mockCache = new Mock<IMemoryCache>();
		var mockLogger = new Mock<ILogger<TenantFeatureService>>();
		var mockCacheEntry = new Mock<ICacheEntry>();

		// Moq: simulate cache miss
		object? notCached = null;
		mockCache
			.Setup(c => c.TryGetValue(It.IsAny<object>(), out notCached))
			.Returns(false);

		// Moq: allow Set() to be called without error
		mockCache
			.Setup(c => c.CreateEntry(It.IsAny<object>()))
			.Returns(mockCacheEntry.Object);
		mockCacheEntry.SetupSet(e => e.Value = It.IsAny<object>());
		mockCacheEntry.SetupSet(e => e.AbsoluteExpirationRelativeToNow = It.IsAny<TimeSpan?>());

		var dbOptions = new DbContextOptionsBuilder<TenantDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		await using var context = new TenantDbContext(dbOptions);

		// Seed the in-memory database
		await context.TenantFeatures.AddAsync(new TenantFeature
		{
			Id = Guid.NewGuid(),
			TenantId = tenantId,
			FeatureKey = featureKey,
			IsEnabled = true,
			RolloutPercentage = 100
		});
		await context.SaveChangesAsync();

		var service = new TenantFeatureService(context, mockCache.Object, mockLogger.Object);

		// Act
		var result = await service.GetFeatureAsync(tenantId, featureKey);

		// Assert
		result.Should().NotBeNull();
		result!.FeatureKey.Should().Be(featureKey);
		result.TenantId.Should().Be(tenantId);
	}
}