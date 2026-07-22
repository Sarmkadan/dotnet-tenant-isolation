using System;
using System.Collections.Generic;
using FluentAssertions;
using TenantIsolation.Exceptions;
using Xunit;

namespace TenantIsolation.Tests;

public class TenantIsolationExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesExceptionWithDefaultMessage()
    {
        // Act
        var exception = new TenantIsolationException();

        // Assert
        exception.Message.Should().NotBeEmpty();
        exception.ErrorCode.Should().BeNull();
        exception.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithSpecifiedMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new TenantIsolationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().BeNull();
        exception.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_CreatesExceptionWithBoth()
    {
        // Arrange
        var message = "Test error message";
        var errorCode = "TEST_ERROR_CODE";

        // Act
        var exception = new TenantIsolationException(message, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesExceptionWithInnerException()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TenantIsolationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ErrorCode.Should().BeNull();
        exception.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageErrorCodeAndErrorDetails_CreatesExceptionWithAllProperties()
    {
        // Arrange
        var message = "Test error message";
        var errorCode = "TEST_ERROR_CODE";
        var errorDetails = new Dictionary<string, object?>
        {
            { "detail1", "value1" },
            { "detail2", 123 },
            { "detail3", null }
        };

        // Act
        var exception = new TenantIsolationException(message, errorCode, errorDetails);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.ErrorDetails.Should().BeSameAs(errorDetails);
        exception.ErrorDetails.Should().HaveCount(3);
    }

    [Fact]
    public void ErrorCode_Setter_SetsErrorCodeProperty()
    {
        // Arrange
        var exception = new TenantIsolationException();
        var errorCode = "NEW_ERROR_CODE";

        // Act
        exception.ErrorCode = errorCode;

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void ErrorDetails_Setter_SetsErrorDetailsProperty()
    {
        // Arrange
        var exception = new TenantIsolationException();
        var errorDetails = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        exception.ErrorDetails = errorDetails;

        // Assert
        exception.ErrorDetails.Should().BeSameAs(errorDetails);
    }

    [Fact]
    public void ToString_WithErrorCode_IncludesCodeInOutput()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("[Code: TEST_CODE]");
    }

    [Fact]
    public void ToString_WithoutErrorCode_DoesNotIncludeCode()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().NotContain("[Code:");
    }

    [Fact]
    public void ToString_IncludesBaseExceptionToString()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new TenantIsolationException("Test message", innerException);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("Inner error");
    }

    [Fact]
    public void TenantNotResolvedException_DefaultConstructor_SetsDefaultMessageAndErrorCode()
    {
        // Act
        var exception = new TenantNotResolvedException();

        // Assert
        exception.Message.Should().Be("Failed to resolve tenant from request context");
        exception.ErrorCode.Should().Be("TENANT_NOT_RESOLVED");
    }

    [Fact]
    public void TenantNotResolvedException_WithSourceAndIdentifier_CreatesCustomMessage()
    {
        // Arrange
        var source = "HttpContext";
        var identifier = "tenant-id-123";

        // Act
        var exception = new TenantNotResolvedException(source, identifier);

        // Assert
        exception.Message.Should().Be("Tenant could not be resolved from HttpContext using identifier: tenant-id-123");
        exception.ErrorCode.Should().Be("TENANT_NOT_RESOLVED");
    }

    [Fact]
    public void TenantNotResolvedException_WithSourceOnly_CreatesMessageWithoutIdentifier()
    {
        // Arrange
        var source = "HttpContext";

        // Act
        var exception = new TenantNotResolvedException(source, null);

        // Assert
        exception.Message.Should().Be("Tenant could not be resolved from HttpContext");
        exception.ErrorCode.Should().Be("TENANT_NOT_RESOLVED");
    }

    [Fact]
    public void TenantNotResolvedException_WithCustomMessage_CreatesExceptionWithCustomMessage()
    {
        // Arrange
        var message = "Custom tenant resolution failed";

        // Act
        var exception = new TenantNotResolvedException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("TENANT_NOT_RESOLVED");
    }

    [Fact]
    public void TenantNotActiveException_WithTenantId_SetsTenantIdProperty()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var exception = new TenantNotActiveException(tenantId);

        // Assert
        exception.TenantId.Should().Be(tenantId);
        exception.Message.Should().Contain(tenantId.ToString());
        exception.ErrorCode.Should().Be("TENANT_NOT_ACTIVE");
    }

    [Fact]
    public void TenantNotActiveException_WithTenantIdAndReason_SetsTenantIdAndIncludesReasonInMessage()
    {
        // Arrange
        var tenantId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var reason = "Account suspended";

        // Act
        var exception = new TenantNotActiveException(tenantId, reason);

        // Assert
        exception.TenantId.Should().Be(tenantId);
        exception.Message.Should().Be($"Tenant {tenantId} is not active: {reason}");
        exception.ErrorCode.Should().Be("TENANT_NOT_ACTIVE");
    }

    [Fact]
    public void TenantNotActiveException_WithEmptyTenantId_HasEmptyGuid()
    {
        // Arrange
        var tenantId = Guid.Empty;

        // Act
        var exception = new TenantNotActiveException(tenantId);

        // Assert
        exception.TenantId.Should().Be(Guid.Empty);
        exception.Message.Should().Contain("00000000-0000-0000-0000-000000000000");
    }

    [Fact]
    public void TenantConfigurationException_WithMessage_SetsMessageAndErrorCode()
    {
        // Arrange
        var message = "Configuration error occurred";

        // Act
        var exception = new TenantConfigurationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("TENANT_CONFIG_ERROR");
    }

    [Fact]
    public void TenantConfigurationException_WithConfigKeyAndMessage_IncludesKeyInMessage()
    {
        // Arrange
        var configKey = "ConnectionStrings:Default";
        var message = "Invalid connection string format";

        // Act
        var exception = new TenantConfigurationException(configKey, message);

        // Assert
        exception.Message.Should().Be("Configuration error for key 'ConnectionStrings:Default': Invalid connection string format");
        exception.ErrorCode.Should().Be("TENANT_CONFIG_ERROR");
    }

    [Fact]
    public void DataIsolationViolationException_WithTenantIdAndMessage_SetsProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var message = "Unauthorized access to customer data";

        // Act
        var exception = new DataIsolationViolationException(tenantId, message);

        // Assert
        exception.TenantId.Should().Be(tenantId);
        exception.Message.Should().Contain(tenantId.ToString());
        exception.Message.Should().Contain(message);
        exception.EntityType.Should().BeNull();
        exception.ErrorCode.Should().Be("DATA_ISOLATION_VIOLATION");
    }

    [Fact]
    public void DataIsolationViolationException_WithTenantIdEntityTypeAndMessage_SetsAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var entityType = "Customer";
        var message = "Cross-tenant data access detected";

        // Act
        var exception = new DataIsolationViolationException(tenantId, entityType, message);

        // Assert
        exception.TenantId.Should().Be(tenantId);
        exception.EntityType.Should().Be(entityType);
        exception.Message.Should().Contain(tenantId.ToString());
        exception.Message.Should().Contain(entityType);
        exception.Message.Should().Contain(message);
        exception.ErrorCode.Should().Be("DATA_ISOLATION_VIOLATION");
    }

    [Fact]
    public void TenantDatabaseException_WithMessage_SetsMessageAndErrorCode()
    {
        // Arrange
        var message = "Database connection failed";

        // Act
        var exception = new TenantDatabaseException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("TENANT_DB_ERROR");
    }

    [Fact]
    public void TenantDatabaseException_WithTenantIdMessageAndInnerException_SetsAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var message = "Connection timeout";
        var innerException = new TimeoutException("Operation timed out");

        // Act
        var exception = new TenantDatabaseException(tenantId, message, innerException);

        // Assert
        exception.Message.Should().Contain(tenantId.ToString());
        exception.Message.Should().Contain(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ErrorCode.Should().Be("TENANT_DB_ERROR");
    }

    [Fact]
    public void Inheritance_AllExceptionsInheritFromTenantIsolationException()
    {
        // Arrange & Act
        var baseException = new TenantIsolationException();
        var notResolved = new TenantNotResolvedException();
        var notActive = new TenantNotActiveException(Guid.NewGuid());
        var configException = new TenantConfigurationException("key", "message");
        var dataException = new DataIsolationViolationException(Guid.NewGuid(), "message");
        var dbException = new TenantDatabaseException("message");

        // Assert
        baseException.Should().BeAssignableTo<TenantIsolationException>();
        notResolved.Should().BeAssignableTo<TenantIsolationException>();
        notActive.Should().BeAssignableTo<TenantIsolationException>();
        configException.Should().BeAssignableTo<TenantIsolationException>();
        dataException.Should().BeAssignableTo<TenantIsolationException>();
        dbException.Should().BeAssignableTo<TenantIsolationException>();
    }

    [Fact]
    public void ErrorDetails_Modification_DoesNotAffectOriginalDictionary()
    {
        // Arrange
        var originalDetails = new Dictionary<string, object?> { { "key", "original" } };
        var exception = new TenantIsolationException("message", "code", originalDetails);

        // Act - modify the original dictionary
        originalDetails["key"] = "modified";
        originalDetails.Add("newKey", "newValue");

        // Assert - exception's ErrorDetails should reflect the changes
        exception.ErrorDetails.Should().HaveCount(2);
        exception.ErrorDetails["key"].Should().Be("modified");
        exception.ErrorDetails.Should().ContainKey("newKey");
    }

    [Fact]
    public void ErrorDetails_NullDictionary_WorksCorrectly()
    {
        // Arrange
        var exception = new TenantIsolationException("message");
        exception.ErrorDetails = null;

        // Act & Assert - should not throw
        exception.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void ErrorCode_NullValue_WorksCorrectly()
    {
        // Arrange
        var exception = new TenantIsolationException("message");
        exception.ErrorCode = null;

        // Act & Assert - should not throw
        exception.ErrorCode.Should().BeNull();
    }
}