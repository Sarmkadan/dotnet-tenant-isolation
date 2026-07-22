using System;
using System.Collections.Generic;
using FluentAssertions;
using TenantIsolation.Exceptions;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantIsolationExceptionExtensionsTests
{
    [Fact]
    public void WithDetail_AddsDetailToException_WhenExceptionHasNoDetails()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        var key = "testKey";
        var value = "testValue";

        // Act
        var result = exception.WithDetail(key, value);

        // Assert
        result.Should().BeSameAs(exception);
        exception.ErrorDetails.Should().NotBeNull();
        exception.ErrorDetails.Should().HaveCount(1);
        exception.ErrorDetails.Should().ContainKeys(key);
        exception.ErrorDetails[key].Should().Be(value);
    }

    [Fact]
    public void WithDetail_AddsDetailToExistingDictionary_WhenExceptionHasDetails()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        exception.WithDetail("existingKey", "existingValue");
        var key = "newKey";
        var value = "newValue";

        // Act
        var result = exception.WithDetail(key, value);

        // Assert
        result.Should().BeSameAs(exception);
        exception.ErrorDetails.Should().HaveCount(2);
        exception.ErrorDetails.Should().ContainKeys(key);
        exception.ErrorDetails[key].Should().Be(value);
    }

    [Fact]
    public void WithDetail_UpdatesExistingKey_WhenKeyAlreadyExists()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        exception.WithDetail("key", "oldValue");
        var value = "newValue";

        // Act
        var result = exception.WithDetail("key", value);

        // Assert
        result.Should().BeSameAs(exception);
        exception.ErrorDetails.Should().ContainKeys("key");
        exception.ErrorDetails["key"].Should().Be(value);
    }

    [Fact]
    public void WithDetail_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;
        var key = "testKey";
        var value = "testValue";

        // Act
        Action act = () => exception.WithDetail(key, value);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDetail_ThrowsArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        string key = null;
        var value = "testValue";

        // Act
        Action act = () => exception.WithDetail(key, value);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDetails_AddsMultipleDetailsAtOnce()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        var details = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "key3", null }
        };

        // Act
        var result = exception.WithDetails(details);

        // Assert
        result.Should().BeSameAs(exception);
        exception.ErrorDetails.Should().NotBeNull();
        exception.ErrorDetails.Should().HaveCount(3);
        exception.ErrorDetails.Should().ContainKeys("key1", "key2", "key3");
        exception.ErrorDetails["key1"].Should().Be("value1");
        exception.ErrorDetails["key2"].Should().Be(123);
        exception.ErrorDetails["key3"].Should().BeNull();
    }

    [Fact]
    public void WithDetails_AddsToExistingDictionary_WhenExceptionHasDetails()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        exception.WithDetail("existing", "value");
        var details = new Dictionary<string, object?> { { "new1", "val1" }, { "new2", "val2" } };

        // Act
        var result = exception.WithDetails(details);

        // Assert
        result.Should().BeSameAs(exception);
        exception.ErrorDetails.Should().HaveCount(3);
    }

    [Fact]
    public void WithDetails_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;
        var details = new Dictionary<string, object?>();

        // Act
        Action act = () => exception.WithDetails(details);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDetails_ThrowsArgumentNullException_WhenDetailsIsNull()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        Dictionary<string, object?> details = null;

        // Act
        Action act = () => exception.WithDetails(details);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithErrorCode_CreatesNewExceptionWithNewErrorCode()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message", "ORIGINAL_CODE");
        originalException.WithDetail("detail", "value");
        var newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = originalException.WithErrorCode(newErrorCode);

        // Assert
        result.Should().NotBeSameAs(originalException);
        result.ErrorCode.Should().Be(newErrorCode);
        result.Message.Should().Be("Original message");
        result.ErrorDetails.Should().NotBeNull();
        result.ErrorDetails.Should().HaveCount(1);
        result.ErrorDetails.Should().ContainKeys("detail");
        result.ErrorDetails["detail"].Should().Be("value");
    }

    [Fact]
    public void WithErrorCode_CopiesAllPropertiesFromOriginalException()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message", "ORIGINAL_CODE");
        originalException.Source = "TestSource";
        originalException.HelpLink = "http://test.com";
        originalException.HResult = -1;
        var newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = originalException.WithErrorCode(newErrorCode);

        // Assert
        result.Source.Should().Be("TestSource");
        result.HelpLink.Should().Be("http://test.com");
        result.HResult.Should().Be(-1);
    }

    [Fact]
    public void WithErrorCode_IncludesOriginalExceptionInData()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message");
        var newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = originalException.WithErrorCode(newErrorCode);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Contains("OriginalException").Should().BeTrue();
        result.Data["OriginalException"].Should().BeSameAs(originalException);
    }

    [Fact]
    public void WithErrorCode_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;
        var newErrorCode = "NEW_CODE";

        // Act
        Action act = () => exception.WithErrorCode(newErrorCode);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithErrorCode_ThrowsArgumentNullException_WhenNewErrorCodeIsNull()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        string newErrorCode = null;

        // Act
        Action act = () => exception.WithErrorCode(newErrorCode);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithContext_CreatesNewExceptionWithAppendedContext()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message", "ORIGINAL_CODE");
        var context = "Additional context information";

        // Act
        var result = originalException.WithContext(context);

        // Assert
        result.Should().NotBeSameAs(originalException);
        result.Message.Should().Be("Original message Additional context information");
        result.ErrorCode.Should().Be("ORIGINAL_CODE");
    }

    [Fact]
    public void WithContext_IncludesContextInData()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message");
        var context = "Additional context";

        // Act
        var result = originalException.WithContext(context);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Contains("Context").Should().BeTrue();
        result.Data["Context"].Should().Be(context);
    }

    [Fact]
    public void WithContext_CopiesAllPropertiesFromOriginalException()
    {
        // Arrange
        var originalException = new TenantIsolationException("Original message", "ORIGINAL_CODE");
        originalException.Source = "TestSource";
        originalException.HelpLink = "http://test.com";
        originalException.HResult = -1;
        var context = "Additional context";

        // Act
        var result = originalException.WithContext(context);

        // Assert
        result.Source.Should().Be("TestSource");
        result.HelpLink.Should().Be("http://test.com");
        result.HResult.Should().Be(-1);
    }

    [Fact]
    public void WithContext_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;
        var context = "context";

        // Act
        Action act = () => exception.WithContext(context);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithContext_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");
        string context = null;

        // Act
        Action act = () => exception.WithContext(context);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetTenantId_ReturnsTrueAndSetsTenantId_ForTenantNotActiveException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var exception = new TenantNotActiveException(tenantId, "Inactive");

        // Act
        var result = exception.TryGetTenantId(out var retrievedTenantId);

        // Assert
        result.Should().BeTrue();
        retrievedTenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TryGetTenantId_ReturnsTrueAndSetsTenantId_ForDataIsolationViolationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var exception = new DataIsolationViolationException(tenantId, "Entity", "Violation message");

        // Act
        var result = exception.TryGetTenantId(out var retrievedTenantId);

        // Assert
        result.Should().BeTrue();
        retrievedTenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TryGetTenantId_ReturnsFalseAndSetsDefault_ForBaseTenantIsolationException()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");

        // Act
        var result = exception.TryGetTenantId(out var retrievedTenantId);

        // Assert
        result.Should().BeFalse();
        retrievedTenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetTenantId_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;

        // Act
        Action act = () => exception.TryGetTenantId(out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetEntityType_ReturnsTrueAndSetsEntityType_ForDataIsolationViolationExceptionWithEntityType()
    {
        // Arrange
        var entityType = "Customer";
        var exception = new DataIsolationViolationException(Guid.NewGuid(), entityType, "Violation message");

        // Act
        var result = exception.TryGetEntityType(out var retrievedEntityType);

        // Assert
        result.Should().BeTrue();
        retrievedEntityType.Should().Be(entityType);
    }

    [Fact]
    public void TryGetEntityType_ReturnsTrueAndSetsNull_ForDataIsolationViolationExceptionWithoutEntityType()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "Violation message");

        // Act
        var result = exception.TryGetEntityType(out var retrievedEntityType);

        // Assert
        result.Should().BeTrue();
        retrievedEntityType.Should().BeNull();
    }

    [Fact]
    public void TryGetEntityType_ReturnsFalseAndSetsNull_ForBaseTenantIsolationException()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");

        // Act
        var result = exception.TryGetEntityType(out var retrievedEntityType);

        // Assert
        result.Should().BeFalse();
        retrievedEntityType.Should().BeNull();
    }

    [Fact]
    public void TryGetEntityType_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        TenantIsolationException exception = null;

        // Act
        Action act = () => exception.TryGetEntityType(out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}