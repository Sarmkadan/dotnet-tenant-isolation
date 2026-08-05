#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Configuration;
using Xunit;

namespace TenantIsolation.Tests;

/// <summary>
/// Test suite for <see cref="ValidationResult"/> and <see cref="ConfigurationValidator"/> functionality.
/// </summary>
public class ValidationResultTests
{
    private readonly Mock<ILogger<ConfigurationValidator>> _mockLogger;

    public ValidationResultTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationValidator>>();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values != null)
        {
            builder.AddInMemoryCollection(values);
        }

        return builder.Build();
    }

    [Fact]
    public void AddError_AddsMessageToErrorsAndDoesNotAffectWarnings()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.AddError("connection string missing");

        // Assert
        result.Errors.Should().ContainSingle().Which.Should().Be("connection string missing");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void AddWarning_AddsMessageToWarningsAndDoesNotAffectErrors()
    {
        // Arrange
        var result = new ValidationResult { IsValid = true };

        // Act
        result.AddWarning("feature flag not configured");

        // Assert
        result.Warnings.Should().ContainSingle().Which.Should().Be("feature flag not configured");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsIsValidTrueWithNoErrors()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Test;",
            ["TenantIsolation:AutoMigrate"] = "true",
            ["TenantIsolation:EnableAuditLogging"] = "true",
            ["TenantIsolation:EnableSoftDeleteFilter"] = "true",
            ["Features:EnableWebhooks"] = "true",
            ["Features:EnableCaching"] = "true",
            ["Features:EnableEventBus"] = "true",
            ["Integration:WebhookUrl"] = "https://example.com/webhook",
            ["Integration:ExternalApiUrl"] = "https://example.com/api"
        });
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingConnectionString_ReturnsIsValidFalseWithError()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DefaultConnection"));
    }

    [Fact]
    public void ValidateSection_WithNullOrWhitespaceSectionName_ThrowsArgumentException()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        Action act = () => validator.ValidateSection("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("sectionName");
    }

    [Fact]
    public void ValidateSection_WithMissingSection_ReturnsIsValidFalseWithError()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        var result = validator.ValidateSection("NonExistentSection");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("NonExistentSection");
    }

    [Fact]
    public void ValidateAndThrow_WithInvalidConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration();
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        Action act = () => validator.ValidateAndThrow();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Configuration validation failed*");
    }

    [Fact]
    public void ValidateAndThrow_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Test;"
        });
        var validator = new ConfigurationValidator(configuration, _mockLogger.Object);

        // Act
        Action act = () => validator.ValidateAndThrow();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddConfigurationValidator_RegistersConfigurationValidatorService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddConfigurationValidator();

        // Assert
        returnedServices.Should().BeSameAs(services);
        services.Should().Contain(d => d.ServiceType == typeof(IConfigurationValidator));
    }
}
