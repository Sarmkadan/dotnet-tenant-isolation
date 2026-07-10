# IHttpClientFactory
The `IHttpClientFactory` interface is a crucial component in the `dotnet-tenant-isolation` project, responsible for creating and managing instances of `HttpClient`. It provides a flexible and efficient way to handle HTTP requests, allowing for customization of client settings, authentication, and other features. This interface is designed to be used in a variety of scenarios, including web applications, microservices, and other distributed systems.

## API
The `IHttpClientFactory` interface exposes several members that enable its functionality:
* `TenantIsolationHttpClientFactory`: A property that returns an instance of `TenantIsolationHttpClientFactory`.
* `CreateClient`: A method that creates a new instance of `HttpClient`. It does not take any parameters and returns an `HttpClient` instance.
* `CreateAuthenticatedClient`: A method that creates a new instance of `HttpClient` with authentication. It does not take any parameters and returns an `HttpClient` instance.
* `GetNamedClient`: A method that retrieves a named instance of `HttpClient`. It does not take any parameters and returns an `HttpClient` instance.
* `TimeoutSeconds`: A property that gets or sets the timeout in seconds for the `HttpClient` instances created by the factory. It is an integer value.
* `MaxRetries`: A property that gets or sets the maximum number of retries for the `HttpClient` instances created by the factory. It is an integer value.
* `AutomaticDecompression`: A property that gets or sets a value indicating whether automatic decompression is enabled for the `HttpClient` instances created by the factory. It is a boolean value.
* `MaxConnectionPoolSize`: A property that gets or sets the maximum size of the connection pool for the `HttpClient` instances created by the factory. It is an integer value.
* `WithHeader`: A static method that creates a new instance of `HttpClient` with a specified header. It takes two parameters: the header name and the header value, and returns an `HttpClient` instance.
* `WithAccept`: A static method that creates a new instance of `HttpClient` with a specified accept header. It takes one parameter: the accept header value, and returns an `HttpClient` instance.
* `WithUserAgent`: A static method that creates a new instance of `HttpClient` with a specified user agent header. It takes one parameter: the user agent header value, and returns an `HttpClient` instance.
* `WithBearerToken`: A static method that creates a new instance of `HttpClient` with a specified bearer token. It takes one parameter: the bearer token value, and returns an `HttpClient` instance.
* `WithTimeout`: A static method that creates a new instance of `HttpClient` with a specified timeout. It takes one parameter: the timeout value, and returns an `HttpClient` instance.
* `AddTenantIsolationHttpClientFactory`: A static method that adds the `TenantIsolationHttpClientFactory` to the specified `IServiceCollection`. It takes one parameter: the `IServiceCollection` instance, and returns the `IServiceCollection` instance.

## Usage
Here are two examples of using the `IHttpClientFactory` interface:
```csharp
// Example 1: Creating a new instance of HttpClient
var httpClientFactory = new TenantIsolationHttpClientFactory();
var httpClient = httpClientFactory.CreateClient();
httpClient.GetAsync("https://example.com");

// Example 2: Creating a new instance of HttpClient with authentication
var authenticatedHttpClientFactory = new TenantIsolationHttpClientFactory();
var authenticatedHttpClient = authenticatedHttpClientFactory.CreateAuthenticatedClient();
authenticatedHttpClient.GetAsync("https://example.com/protected");
```

## Notes
When using the `IHttpClientFactory` interface, it is essential to consider the following edge cases and thread-safety remarks:
* The `CreateClient` and `CreateAuthenticatedClient` methods create new instances of `HttpClient`, which can lead to socket exhaustion if not properly disposed of. It is recommended to use the `using` statement or `Dispose` method to ensure proper disposal.
* The `GetNamedClient` method retrieves a named instance of `HttpClient`, which can be shared across multiple requests. However, this can also lead to issues if the named client is not properly configured or disposed of.
* The `TimeoutSeconds`, `MaxRetries`, `AutomaticDecompression`, and `MaxConnectionPoolSize` properties are used to configure the `HttpClient` instances created by the factory. These properties should be carefully considered to ensure optimal performance and reliability.
* The static methods `WithHeader`, `WithAccept`, `WithUserAgent`, `WithBearerToken`, and `WithTimeout` create new instances of `HttpClient` with specified settings. These methods can be used to create clients with custom settings, but it is essential to ensure that the created clients are properly disposed of to avoid socket exhaustion.
* The `AddTenantIsolationHttpClientFactory` method adds the `TenantIsolationHttpClientFactory` to the specified `IServiceCollection`. This method should be used carefully to avoid conflicts with other services or factories.
* The `IHttpClientFactory` interface is designed to be thread-safe, but it is still essential to ensure that the created `HttpClient` instances are properly synchronized and disposed of to avoid issues in multi-threaded environments.
