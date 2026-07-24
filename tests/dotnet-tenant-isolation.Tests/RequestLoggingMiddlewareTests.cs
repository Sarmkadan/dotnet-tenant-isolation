#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Middleware;
using Xunit;

namespace TenantIsolation.Tests;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<ILogger<RequestLoggingMiddleware>> _loggerMock = new();
    private readonly Mock<RequestDelegate> _nextMock = new();

    [Fact]
    public void Constructor_WithNullNext_DoesNotThrow()
    {
        // Arrange
        RequestDelegate? next = null;
        var logger = _loggerMock.Object;

        // Act - middleware doesn't throw for null next (ASP.NET Core pattern)
        var middleware = new RequestLoggingMiddleware(next!, logger);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var next = _nextMock.Object;
        ILogger<RequestLoggingMiddleware>? logger = null;

        // Act - middleware doesn't throw for null logger (ASP.NET Core pattern)
        var middleware = new RequestLoggingMiddleware(next, logger!);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WithValidContext_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();

        var middleware = new RequestLoggingMiddleware(_nextMock.Object, _loggerMock.Object);

        // Setup next delegate to complete successfully
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify();
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulRequest_LogsIncomingRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/users";
        context.Request.Method = "GET";
        context.Request.QueryString = new QueryString("?id=123");
        context.Request.Headers.UserAgent = "Test-Agent";
        context.Connection.RemoteIpAddress = new System.Net.IPAddress(123456789);
        context.Items["TenantId"] = "tenant-123";
        context.Response.Body = new MemoryStream();

        var middleware = new RequestLoggingMiddleware(_nextMock.Object, _loggerMock.Object);

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[REQUEST] GET /api/users?id=123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulRequest_LogsOutgoingResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "POST";
        context.Response.StatusCode = 200;
        context.Response.Body = new MemoryStream();
        context.Items["TenantId"] = "tenant-456";

        var middleware = new RequestLoggingMiddleware(_nextMock.Object, _loggerMock.Object);

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[RESPONSE] 200 (SUCCESS) POST /api/test")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_LogsErrorAndReThrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/exception";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        context.Items["TenantId"] = "tenant-ex";

        var exception = new InvalidOperationException("Test exception");
        var middleware = new RequestLoggingMiddleware(_nextMock.Object, _loggerMock.Object);

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Throws(exception);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        actualException.Should().BeSameAs(exception);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception occurred during request processing")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public void UseRequestLogging_WithValidBuilder_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var builder = new ApplicationBuilder(serviceProvider);

        // Act
        var result = builder.UseRequestLogging();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyPath_LogsRequestWithoutQueryString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        context.Items["TenantId"] = "tenant-root";

        var middleware = new RequestLoggingMiddleware(_nextMock.Object, _loggerMock.Object);

        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[REQUEST] GET /")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }
}
