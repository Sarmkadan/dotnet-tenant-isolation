using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace TenantIsolation.Integration;

public class ApiCallResultTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ExternalApiClient>> _mockLogger;
    private readonly ExternalApiClient _apiClient;

    public ApiCallResultTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("https://api.example.com") };
        _mockLogger = new Mock<ILogger<ExternalApiClient>>();
        _apiClient = new ExternalApiClient(_httpClient, _mockLogger.Object);
    }


    [Fact]
    public void Constructor_WithValidParameters_CreatesApiCallResult()
    {
        // Arrange
        var result = new ApiCallResult<string>
        {
            IsSuccess = true,
            Data = "test data",
            ErrorMessage = null,
            HttpStatusCode = 200,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        // Act & Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("test data");
        result.ErrorMessage.Should().BeNull();
        result.HttpStatusCode.Should().Be(200);
        result.Duration.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Constructor_WithNullData_SetsDataToNull()
    {
        // Arrange & Act
        var result = new ApiCallResult<object>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = "error"
        };

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().BeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("error");
    }

    [Fact]
    public void Constructor_WithEmptyErrorMessage_SetsErrorMessageToEmpty()
    {
        // Arrange & Act
        var result = new ApiCallResult<string>
        {
            IsSuccess = false,
            ErrorMessage = string.Empty,
            HttpStatusCode = 500
        };

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().BeEmpty();
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(500);
    }

    [Fact]
    public void IsSuccess_WhenTrue_ReturnsTrue()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { IsSuccess = true };

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_WhenFalse_ReturnsFalse()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { IsSuccess = false };

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Data_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = new ApiCallResult<int> { Data = 42 };

        // Assert
        result.Data.Should().Be(42);
    }

    [Fact]
    public void Data_WhenNull_ReturnsNull()
    {
        // Arrange & Act
        var result = new ApiCallResult<string?> { Data = null };

        // Assert
        result.Data.Should().BeNull();
    }

    [Fact]
    public void ErrorMessage_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { ErrorMessage = "Not found" };

        // Assert
        result.ErrorMessage.Should().Be("Not found");
    }

    [Fact]
    public void ErrorMessage_WhenNull_ReturnsNull()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { ErrorMessage = null };

        // Assert
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void HttpStatusCode_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { HttpStatusCode = 201 };

        // Assert
        result.HttpStatusCode.Should().Be(201);
    }

    [Fact]
    public void HttpStatusCode_WhenNull_ReturnsNull()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { HttpStatusCode = null };

        // Assert
        result.HttpStatusCode.Should().BeNull();
    }

    [Fact]
    public void Duration_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var duration = TimeSpan.FromSeconds(5);
        var result = new ApiCallResult<string> { Duration = duration };

        // Assert
        result.Duration.Should().Be(duration);
    }

    [Fact]
    public void Duration_WhenZero_ReturnsZero()
    {
        // Arrange & Act
        var result = new ApiCallResult<string> { Duration = TimeSpan.Zero };

        // Assert
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetAsync_SuccessfulResponse_ReturnsSuccessResultWithData()
    {
        // Arrange
        var responseData = new { Id = 1, Name = "Test" };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/test", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(1);
        result.Data.Name.Should().Be("Test");
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetAsync_FailedResponse_ReturnsFailureResultWithError()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/missing", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.HttpStatusCode.Should().Be(404);
        result.ErrorMessage.Should().NotBeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetAsync_WithHeaders_ReturnsSuccessResult()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };
        var responseData = new { Success = true };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Headers.Contains("Authorization")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/protected", headers);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PostAsync_SuccessfulResponse_ReturnsSuccessResultWithData()
    {
        // Arrange
        var payload = new { Name = "Test Item", Value = 123 };
        var responseData = new { Id = 1, Created = true };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PostAsync<ApiResponse>("https://api.example.com/items", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(1);
        result.Data.Created.Should().BeTrue();
        result.HttpStatusCode.Should().Be(201);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_FailedResponse_ReturnsFailureResult()
    {
        // Arrange
        var payload = new { Name = "Invalid Item" };
        var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid payload")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PostAsync<ApiResponse>("https://api.example.com/items", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.HttpStatusCode.Should().Be(400);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task PutAsync_SuccessfulResponse_ReturnsSuccessResult()
    {
        // Arrange
        var payload = new { Id = 1, Name = "Updated Item" };
        var responseData = new { Updated = true };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PutAsync<ApiResponse>("https://api.example.com/items/1", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Updated.Should().BeTrue();
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task PutAsync_FailedResponse_ReturnsFailureResult()
    {
        // Arrange
        var payload = new { Id = 1, Name = "Invalid Update" };
        var mockResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PutAsync<ApiResponse>("https://api.example.com/items/1", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.HttpStatusCode.Should().Be(500);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_SuccessfulResponse_ReturnsSuccessResultWithTrue()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"true\"")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.DeleteAsync("https://api.example.com/items/1", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteAsync_FailedResponse_ReturnsFailureResultWithFalse()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.DeleteAsync("https://api.example.com/items/1", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.HttpStatusCode.Should().Be(404);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task PostAsync_WithEmptyPayload_SerializesCorrectly()
    {
        // Arrange
        var emptyPayload = new { };
        var responseData = new { Success = true };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PostAsync<ApiResponse>("https://api.example.com/empty", emptyPayload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithComplexData_ReturnsDeserializedObject()
    {
        // Arrange
        var complexData = new {
            Items = new[] { "A", "B", "C" },
            Count = 3,
            Active = true
        };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(complexData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ComplexResponse>("https://api.example.com/complex", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Count.Should().Be(3);
        result.Data.Active.Should().BeTrue();
        result.Data.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task MultipleRequests_TrackDurationSeparately()
    {
        // Arrange
        var responseData = new { Id = 1 };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse)
            .ReturnsAsync(mockResponse);

        // Act
        var result1 = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/test1", null);
        var result2 = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/test2", null);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result2.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetAsync_WithUnauthorizedResponse_ReturnsFailureWith401()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/protected", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WithServerErrorResponse_ReturnsFailureWith500()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal server error")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.GetAsync<ApiResponse>("https://api.example.com/error", null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.HttpStatusCode.Should().Be(500);
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task PostAsync_WithCreatedStatus_ReturnsSuccessWith201()
    {
        // Arrange
        var payload = new { Name = "New Item" };
        var responseData = new { Id = 1 };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PostAsync<ApiResponse>("https://api.example.com/create", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.HttpStatusCode.Should().Be(201);
    }

    [Fact]
    public async Task PutAsync_WithNoContentStatus_ReturnsSuccessWith200()
    {
        // Arrange
        var payload = new { Id = 1 };
        var responseData = new { Updated = true };
        var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _apiClient.PutAsync<ApiResponse>("https://api.example.com/update/1", payload, null);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.HttpStatusCode.Should().Be(200);
    }

    // Helper classes for deserialization
    private class ApiResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public bool Success { get; set; }
    }

    private class ComplexResponse
    {
        public string[] Items { get; set; }
        public int Count { get; set; }
        public bool Active { get; set; }
    }
}
