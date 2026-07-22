using FluentAssertions;
using Xunit;
using TenantIsolation.Exceptions;

namespace TenantIsolation.Tests.Exceptions;

public class TenantIsolationExceptionValidationTests
{
    [Fact]
    public void Validate_TenantIsolationException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE")
        {
            ErrorDetails = new Dictionary<string, object?> { { "key1", "value1" }, { "key2", 123 } }
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantIsolationException_WithNullErrorCode_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message")
        {
            ErrorCode = null,
            ErrorDetails = null
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantIsolationException_WithEmptyErrorCode_ReturnsError()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message")
        {
            ErrorCode = "",
            ErrorDetails = null
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("ErrorCode cannot be an empty string");
    }

    [Fact]
    public void Validate_TenantIsolationException_WithEmptyErrorDetailsDictionary_ReturnsError()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE")
        {
            ErrorDetails = new Dictionary<string, object?>()
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("ErrorDetails dictionary cannot be empty");
    }

    [Fact]
    public void Validate_TenantIsolationException_WithEmptyKeyInErrorDetails_ReturnsError()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE")
        {
            ErrorDetails = new Dictionary<string, object?> { { "", "value1" }, { "key2", "value2" } }
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("ErrorDetails contains an entry with null or empty key");
    }

    [Fact]
    public void Validate_TenantIsolationException_WithNullKeyInErrorDetails_ReturnsError()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE")
        {
            ErrorDetails = new Dictionary<string, object?> { { null!, "value1" }, { "key2", "value2" } }
        };

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("ErrorDetails contains an entry with null or empty key");
    }

    [Fact]
    public void Validate_TenantIsolationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantIsolationException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_TenantIsolationException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_TenantIsolationException_WithInvalidException_ReturnsFalse()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message")
        {
            ErrorCode = ""
        };

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_TenantIsolationException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        TenantIsolationException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_TenantIsolationException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message", "TEST_CODE");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_TenantIsolationException_WithInvalidException_ThrowsArgumentException()
    {
        // Arrange
        var exception = new TenantIsolationException("Test message")
        {
            ErrorCode = ""
        };

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ErrorCode cannot be an empty string*");
    }

    [Fact]
    public void EnsureValid_TenantIsolationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantIsolationException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_TenantNotResolvedException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantNotResolvedException();

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantNotResolvedException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantNotResolvedException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_TenantNotResolvedException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new TenantNotResolvedException();

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_TenantNotResolvedException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        TenantNotResolvedException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_TenantNotResolvedException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new TenantNotResolvedException();

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_TenantNotResolvedException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantNotResolvedException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_TenantNotActiveException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.NewGuid());

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantNotActiveException_WithEmptyTenantId_ReturnsError()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.Empty);

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("TenantId cannot be an empty GUID");
    }

    [Fact]
    public void Validate_TenantNotActiveException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantNotActiveException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_TenantNotActiveException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.NewGuid());

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_TenantNotActiveException_WithEmptyTenantId_ReturnsFalse()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.Empty);

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_TenantNotActiveException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        TenantNotActiveException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_TenantNotActiveException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.NewGuid());

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_TenantNotActiveException_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var exception = new TenantNotActiveException(Guid.Empty);

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TenantId cannot be an empty GUID*");
    }

    [Fact]
    public void EnsureValid_TenantNotActiveException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantNotActiveException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_TenantConfigurationException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantConfigurationException("configKey", "Test message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantConfigurationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantConfigurationException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_TenantConfigurationException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new TenantConfigurationException("configKey", "Test message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_TenantConfigurationException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        TenantConfigurationException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_TenantConfigurationException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new TenantConfigurationException("configKey", "Test message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_TenantConfigurationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantConfigurationException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_DataIsolationViolationException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "EntityType", "Test message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_DataIsolationViolationException_WithEmptyTenantId_ReturnsError()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.Empty, "EntityType", "Test message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("TenantId cannot be an empty GUID");
    }

    [Fact]
    public void Validate_DataIsolationViolationException_WithEmptyEntityType_ReturnsError()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "", "Test message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("EntityType cannot be an empty string");
    }

    [Fact]
    public void Validate_DataIsolationViolationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        DataIsolationViolationException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_DataIsolationViolationException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "EntityType", "Test message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_DataIsolationViolationException_WithEmptyTenantId_ReturnsFalse()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.Empty, "EntityType", "Test message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_DataIsolationViolationException_WithEmptyEntityType_ReturnsFalse()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "", "Test message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_DataIsolationViolationException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        DataIsolationViolationException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_DataIsolationViolationException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "EntityType", "Test message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_DataIsolationViolationException_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.Empty, "EntityType", "Test message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TenantId cannot be an empty GUID*");
    }

    [Fact]
    public void EnsureValid_DataIsolationViolationException_WithEmptyEntityType_ThrowsArgumentException()
    {
        // Arrange
        var exception = new DataIsolationViolationException(Guid.NewGuid(), "", "Test message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*EntityType cannot be an empty string*");
    }

    [Fact]
    public void EnsureValid_DataIsolationViolationException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        DataIsolationViolationException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_TenantDatabaseException_WithValidException_ReturnsEmptyList()
    {
        // Arrange
        var exception = new TenantDatabaseException(Guid.NewGuid(), "Test message", new Exception("Inner"));

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_TenantDatabaseException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantDatabaseException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_TenantDatabaseException_WithValidException_ReturnsTrue()
    {
        // Arrange
        var exception = new TenantDatabaseException(Guid.NewGuid(), "Test message", new Exception("Inner"));

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_TenantDatabaseException_WithNullInput_ReturnsFalse()
    {
        // Arrange
        TenantDatabaseException? exception = null;

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_TenantDatabaseException_WithValidException_DoesNotThrow()
    {
        // Arrange
        var exception = new TenantDatabaseException(Guid.NewGuid(), "Test message", new Exception("Inner"));

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_TenantDatabaseException_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        TenantDatabaseException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}