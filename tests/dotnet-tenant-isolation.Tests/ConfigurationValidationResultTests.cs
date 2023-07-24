// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using TenantIsolation.Configuration;

namespace TenantIsolation.Tests;

public class ConfigurationValidationResultTests
{
    [Fact]
    public void Constructor_DefaultsToValid()
    {
        var result = new ValidationResult();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void AddError_AddsToErrorList()
    {
        var result = new ValidationResult();
        result.AddError("Missing connection string");
        result.Errors.Should().ContainSingle("Missing connection string");
    }

    [Fact]
    public void AddWarning_AddsToWarningList()
    {
        var result = new ValidationResult();
        result.AddWarning("Default tenant limit used");
        result.Warnings.Should().ContainSingle("Default tenant limit used");
    }

    [Fact]
    public void AddMultipleErrors_AllAreTracked()
    {
        var result = new ValidationResult();
        result.AddError("Error 1");
        result.AddError("Error 2");
        result.AddError("Error 3");
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void IsValid_WithErrors_CanBeSetToFalse()
    {
        var result = new ValidationResult { IsValid = true };
        result.AddError("critical error");
        result.IsValid = result.Errors.Count == 0;
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_NoErrors_RemainsTrue()
    {
        var result = new ValidationResult { IsValid = true };
        result.AddWarning("just a warning");
        result.IsValid = result.Errors.Count == 0;
        result.IsValid.Should().BeTrue();
    }
}
