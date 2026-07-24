// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using FluentAssertions;
using TenantIsolation.Models;
using Xunit;

namespace TenantIsolation.Tests;

public sealed class TenantConnectionStringTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private const string TestDatabaseType = "SqlServer";
    private const string TestConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;Connection Timeout=30";
    private const string TestName = "Primary Connection";
    private const string TestSchemaName = "dbo";
    private const string TestDatabaseName = "TestDatabase";
    private const string TestServerHost = "db.example.com";
    private const int TestServerPort = 1433;
    private const int TestConnectionTimeout = 60;

    private static TenantConnectionString CreateValidConnectionString()
    {
        return new TenantConnectionString
        {
            Id = TestId,
            TenantId = TestTenantId,
            DatabaseType = TestDatabaseType,
            ConnectionString = TestConnectionString,
            Name = TestName,
            SchemaName = TestSchemaName,
            DatabaseName = TestDatabaseName,
            ServerHost = TestServerHost,
            ServerPort = TestServerPort,
            ConnectionTimeout = TestConnectionTimeout
        };
    }

    [Fact]
    public void Constructor_Default_CreatesInstanceWithDefaultValues()
    {
        // Arrange & Act
        var connectionString = new TenantConnectionString();

        // Assert
        Assert.Equal(Guid.Empty, connectionString.Id);
        Assert.Equal(Guid.Empty, connectionString.TenantId);
        Assert.Equal("SqlServer", connectionString.DatabaseType);
        Assert.Null(connectionString.ConnectionString);
        Assert.Null(connectionString.Name);
        Assert.Null(connectionString.SchemaName);
        Assert.Null(connectionString.DatabaseName);
        Assert.Null(connectionString.ServerHost);
        Assert.Null(connectionString.ServerPort);
        Assert.Equal(30, connectionString.ConnectionTimeout);
        Assert.Equal(300, connectionString.CommandTimeout);
        Assert.Equal(100, connectionString.MaxPoolSize);
        Assert.True(connectionString.UseConnectionPooling);
        Assert.True(connectionString.IsPrimary);
        Assert.True(connectionString.IsActive);
        Assert.NotEqual(default, connectionString.CreatedAt);
        Assert.Null(connectionString.LastTestedAt);
        Assert.Null(connectionString.LastTestResult);
    }

    [Fact]
    public void Id_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testId = Guid.NewGuid();

        // Act
        connectionString.Id = testId;

        // Assert
        Assert.Equal(testId, connectionString.Id);
    }

    [Fact]
    public void TenantId_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testTenantId = Guid.NewGuid();

        // Act
        connectionString.TenantId = testTenantId;

        // Assert
        Assert.Equal(testTenantId, connectionString.TenantId);
    }

    [Fact]
    public void DatabaseType_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();

        // Act
        connectionString.DatabaseType = "PostgreSQL";

        // Assert
        Assert.Equal("PostgreSQL", connectionString.DatabaseType);
    }

    [Fact]
    public void ConnectionString_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testConnectionString = "Server=test;Database=test;User Id=test;Password=test;";

        // Act
        connectionString.ConnectionString = testConnectionString;

        // Assert
        Assert.Equal(testConnectionString, connectionString.ConnectionString);
    }

    [Fact]
    public void Name_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testName = "Test Connection";

        // Act
        connectionString.Name = testName;

        // Assert
        Assert.Equal(testName, connectionString.Name);
    }

    [Fact]
    public void SchemaName_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testSchemaName = "custom_schema";

        // Act
        connectionString.SchemaName = testSchemaName;

        // Assert
        Assert.Equal(testSchemaName, connectionString.SchemaName);
    }

    [Fact]
    public void DatabaseName_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();

        // Act
        connectionString.DatabaseName = "MyDatabase";

        // Assert
        Assert.Equal("MyDatabase", connectionString.DatabaseName);
    }

    [Fact]
    public void ServerHost_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();
        var testServerHost = "production.db.com";

        // Act
        connectionString.ServerHost = testServerHost;

        // Assert
        Assert.Equal(testServerHost, connectionString.ServerHost);
    }

    [Fact]
    public void ServerPort_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();

        // Act
        connectionString.ServerPort = 5432;

        // Assert
        Assert.Equal(5432, connectionString.ServerPort);
    }

    [Fact]
    public void ConnectionTimeout_GetSet_Roundtrip()
    {
        // Arrange
        var connectionString = new TenantConnectionString();

        // Act
        connectionString.ConnectionTimeout = 120;

        // Assert
        Assert.Equal(120, connectionString.ConnectionTimeout);
    }

    [Fact]
    public void GetTestConnectionString_WithValidConnection_ReturnsModifiedString()
    {
        // Arrange
        var connectionString = CreateValidConnectionString();

        // Act
        var testConnectionString = connectionString.GetTestConnectionString();

        // Assert
        Assert.NotNull(testConnectionString);
        Assert.True(testConnectionString.Contains("Connection Timeout=5"));
    }

    [Fact]
    public void ExtractHostname_WithServerHost_ReturnsServerHost()
    {
        // Arrange
        var connectionString = CreateValidConnectionString();

        // Act
        var hostname = connectionString.ExtractHostname();

        // Assert
        Assert.Equal(TestServerHost, hostname);
    }

    [Fact]
    public void ExtractHostname_WithoutServerHost_ExtractsFromConnectionString()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = "Server=localhost;Database=TestDb;"
        };

        // Act
        var hostname = connectionString.ExtractHostname();

        // Assert
        Assert.Equal("localhost", hostname);
    }


    [Fact]
    public void ExtractHostname_WithEmptyConnectionString_ReturnsUnknown()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = ""
        };

        // Act
        var hostname = connectionString.ExtractHostname();

        // Assert
        Assert.Equal("unknown", hostname);
    }


    [Fact]
    public void IsValidConnectionString_WithValidConnectionString_ReturnsTrue()
    {
        // Arrange
        var connectionString = CreateValidConnectionString();

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithNullConnectionString_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = null
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Connection string cannot be empty", errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithEmptyConnectionString_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = ""
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Connection string cannot be empty", errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithWhitespaceConnectionString_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = "   "
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Connection string cannot be empty", errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithTooShortConnectionString_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = "short"
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Connection string appears to be invalid (too short)", errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithInvalidSqlServerConnectionString_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            DatabaseType = "SqlServer",
            ConnectionString = "Database=TestDb;User Id=test;" // Missing Server/Data Source
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("SQL Server connection string must contain Server or Data Source parameter", errorMessage);
    }

    [Fact]
    public void IsValidConnectionString_WithOutOfRangeConnectionTimeout_ReturnsFalseWithError()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = TestConnectionString,
            ConnectionTimeout = 2 // Below minimum of 5
        };

        // Act
        var isValid = connectionString.IsValidConnectionString(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Connection timeout must be between 5 and 300 seconds", errorMessage);
    }

    [Fact]
    public void RecordSuccessfulTest_SetsLastTestedAtAndLastTestResult()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            LastTestedAt = null,
            LastTestResult = null
        };

        // Act
        connectionString.RecordSuccessfulTest();

        // Assert
        Assert.NotNull(connectionString.LastTestedAt);
        Assert.True(connectionString.LastTestResult);
        Assert.True(connectionString.IsActive);
    }

    [Fact]
    public void RecordFailedTest_SetsLastTestedAtLastTestResultAndIsActive()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            LastTestedAt = null,
            LastTestResult = null,
            IsActive = true
        };

        // Act
        connectionString.RecordFailedTest();

        // Assert
        Assert.NotNull(connectionString.LastTestedAt);
        Assert.False(connectionString.LastTestResult);
        Assert.False(connectionString.IsActive);
    }

    [Fact]
    public void ToString_WithValidConnectionString_RedactsSensitiveData()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            Id = TestId,
            TenantId = TestTenantId,
            DatabaseType = TestDatabaseType,
            ConnectionString = "Server=localhost;Database=Test;User Id=admin;Password=secret123;",
            Name = TestName,
            IsPrimary = true,
            IsActive = true
        };

        // Act
        var result = connectionString.ToString();

        // Assert
        Assert.Contains(TestId.ToString(), result);
        Assert.Contains(TestTenantId.ToString(), result);
        Assert.Contains(TestDatabaseType, result);
        Assert.Contains("***REDACTED***", result);
        Assert.Contains(TestName, result);
        Assert.Contains("IsPrimary=True", result);
        Assert.Contains("IsActive=True", result);
        Assert.DoesNotContain("secret123", result);
        Assert.DoesNotContain("admin", result);
    }

    [Fact]
    public void ToString_WithNullConnectionString_ReturnsRedactedFormat()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            Id = TestId,
            TenantId = TestTenantId,
            ConnectionString = null
        };

        // Act
        var result = connectionString.ToString();

        // Assert
        Assert.Contains("[ConnectionString: null]", result);
    }


    [Fact]
    public void GetRedactedConnectionString_WithNullConnectionString_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = null
        };

        // Act
        var result = connectionString.GetRedactedConnectionString();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRedactedConnectionString_WithInvalidConnectionString_ReturnsErrorMessage()
    {
        // Arrange
        var connectionString = new TenantConnectionString
        {
            ConnectionString = "invalid connection string format"
        };

        // Act
        var result = connectionString.GetRedactedConnectionString();

        // Assert
        Assert.Equal("[Invalid Connection String]", result);
    }
}