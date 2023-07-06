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

public class ValidationUtilityTests
{
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

    [Fact]
    public void RequireNotEmpty_WhenNull_ThrowsTenantIsolationExceptionMentioningFieldName()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequireNotEmpty(null, "TenantName");

        // Assert
        act.Should().Throw<TenantIsolationException>()
            .WithMessage("*TenantName*");
    }

    [Fact]
    public void RequireNotEmpty_WhenWhitespaceOnly_ThrowsTenantIsolationException()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequireNotEmpty("   ", "TenantName");

        // Assert
        act.Should().Throw<TenantIsolationException>();
    }

    [Fact]
    public void RequirePositive_WhenZero_ThrowsTenantIsolationException()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequirePositive(0, "MaxUsers");

        // Assert
        act.Should().Throw<TenantIsolationException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void RequirePositive_WhenPositiveValue_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequirePositive(1, "MaxUsers");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireRange_WhenValueBelowMinimum_ThrowsTenantIsolationException()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequireRange(0, 1, 100, "Percentage");

        // Assert
        act.Should().Throw<TenantIsolationException>()
            .WithMessage("*between*");
    }

    [Fact]
    public void RequireRange_WhenValueAboveMaximum_ThrowsTenantIsolationException()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequireRange(101, 0, 100, "Percentage");

        // Assert
        act.Should().Throw<TenantIsolationException>();
    }

    [Fact]
    public void RequireValidSlug_WhenInvalidFormat_ThrowsExceptionWithHelpfulMessage()
    {
        // Arrange & Act
        var act = () => ValidationUtility.RequireValidSlug("INVALID SLUG!");

        // Assert
        act.Should().Throw<TenantIsolationException>()
            .WithMessage("*not a valid slug*");
    }

    [Fact]
    public void IsValidGuid_WithWellFormedGuid_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();

        // Act & Assert
        ValidationUtility.IsValidGuid(guid).Should().BeTrue();
    }

    [Fact]
    public void IsValidGuid_WithMalformedString_ReturnsFalse()
    {
        // Act & Assert
        ValidationUtility.IsValidGuid("not-a-guid").Should().BeFalse();
    }
}

public class StringExtensionTests
{
    [Fact]
    public void ToSlug_WithSpacesAndUppercase_ReturnsLowercaseHyphenated()
    {
        // Act
        var result = "Hello World".ToSlug();

        // Assert
        result.Should().Be("hello-world");
    }

    [Fact]
    public void ToSlug_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = "".ToSlug();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToSlug_WithSpecialCharacters_RemovesNonAlphanumeric()
    {
        // Act
        var result = "Acme & Co.".ToSlug();

        // Assert
        result.Should().NotContain("&").And.NotContain(".");
        result.Should().MatchRegex(@"^[a-z0-9\-]+$");
    }

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

    [Fact]
    public void IsValidEmail_ExtensionMethod_WithValidAddress_ReturnsTrue()
    {
        // Act & Assert
        "admin@company.com".IsValidEmail().Should().BeTrue();
    }

    [Fact]
    public void IsValidUrl_WithHttpsScheme_ReturnsTrue()
    {
        // Act & Assert
        "https://api.example.com/v1".IsValidUrl().Should().BeTrue();
    }

    [Fact]
    public void IsValidUrl_WithFtpScheme_ReturnsFalse()
    {
        // Act & Assert — only http/https are accepted
        "ftp://files.example.com".IsValidUrl().Should().BeFalse();
    }

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
