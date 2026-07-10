# TenantConnectionString
The `TenantConnectionString` type represents a connection string for a specific tenant in a multi-tenancy environment. It encapsulates various properties related to the connection, such as the database type, connection string, timeouts, and pooling settings. This type is designed to provide a centralized and organized way to manage tenant-specific connection settings, making it easier to implement and maintain tenant isolation in applications.

## API
The `TenantConnectionString` type exposes the following public members:
* `Id`: A unique identifier for the connection string, represented as a `Guid`.
* `TenantId`: The identifier of the tenant associated with this connection string, represented as a `Guid`.
* `DatabaseType`: The type of database being connected to, represented as a `string`.
* `ConnectionString`: The actual connection string used to establish a connection to the database, represented as a `string`.
* `Name`: An optional name for the connection string, represented as a `string?`.
* `SchemaName`: An optional schema name for the connection string, represented as a `string?`.
* `DatabaseName`: An optional database name for the connection string, represented as a `string?`.
* `ServerHost`: An optional server host for the connection string, represented as a `string?`.
* `ServerPort`: An optional server port for the connection string, represented as an `int?`.
* `ConnectionTimeout`: The timeout for establishing a connection to the database, represented as an `int`.
* `CommandTimeout`: The timeout for executing commands on the database, represented as an `int`.
* `MaxPoolSize`: The maximum size of the connection pool, represented as an `int`.
* `UseConnectionPooling`: A flag indicating whether connection pooling is enabled, represented as a `bool`.
* `IsPrimary`: A flag indicating whether this connection string is the primary one for the tenant, represented as a `bool`.
* `IsActive`: A flag indicating whether this connection string is active, represented as a `bool`.
* `CreatedAt`: The date and time when the connection string was created, represented as a `DateTime`.
* `LastTestedAt`: The date and time when the connection string was last tested, represented as a `DateTime?`.
* `LastTestResult`: The result of the last test performed on the connection string, represented as a `bool?`.
* `Tenant`: The tenant associated with this connection string, represented as a `Tenant?`.
* `GetTestConnectionString`: A property that returns a test connection string, represented as a `string`.

## Usage
Here are two examples of using the `TenantConnectionString` type:
```csharp
// Example 1: Creating a new TenantConnectionString instance
var connectionString = new TenantConnectionString
{
    Id = Guid.NewGuid(),
    TenantId = Guid.NewGuid(),
    DatabaseType = "SqlServer",
    ConnectionString = "Server=myserver;Database=mydatabase;User Id=myuser;Password=mypassword;",
    ConnectionTimeout = 30,
    CommandTimeout = 60,
    MaxPoolSize = 100,
    UseConnectionPooling = true,
    IsPrimary = true,
    IsActive = true
};

// Example 2: Retrieving the test connection string
var testConnectionString = connectionString.GetTestConnectionString;
Console.WriteLine(testConnectionString);
```

## Notes
When working with the `TenantConnectionString` type, consider the following edge cases and thread-safety remarks:
* The `Tenant` property may return null if the associated tenant is not available.
* The `LastTestedAt` and `LastTestResult` properties may return null if the connection string has not been tested.
* The `GetTestConnectionString` property may throw an exception if the connection string is not valid.
* The `TenantConnectionString` type is not thread-safe by default. If you need to access or modify instances of this type from multiple threads, consider implementing synchronization mechanisms to ensure thread safety.
* When using connection pooling, be aware that the `MaxPoolSize` property controls the maximum number of connections that can be established to the database. If this limit is exceeded, additional connection requests may be queued or rejected.
