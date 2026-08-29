#nullable enable

using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Middleware;
using Xunit;

namespace TenantIsolation.Tests;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_RequestUnderLimit_CallsNextAndSetsRateLimitHeaders()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var options = new RateLimitOptions { RequestsPerMinute = 3 };
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, options);
        var context = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextMock.Verify();
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("3");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("2");
        DateTime.TryParse(context.Response.Headers["X-RateLimit-Reset"].ToString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_RequestsPerMinuteExceeded_ReturnsTooManyRequestsResponse()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var options = new RateLimitOptions
        {
            RequestsPerMinute = 1,
            RetryAfterSeconds = 45
        };
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, options);

        await middleware.InvokeAsync(CreateContext("tenant-a", IPAddress.Parse("192.0.2.1")));
        var rejectedContext = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"));

        // Act
        await middleware.InvokeAsync(rejectedContext);

        // Assert
        rejectedContext.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        rejectedContext.Response.ContentType.Should().Be("application/json");
        rejectedContext.Response.Headers.RetryAfter.ToString().Should().Be("45");
        nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Once);

        rejectedContext.Response.Body.Position = 0;
        using var responseJson = await JsonDocument.ParseAsync(rejectedContext.Response.Body);
        responseJson.RootElement.GetProperty("code").GetString().Should().Be("RATE_LIMIT_EXCEEDED");
        responseJson.RootElement.GetProperty("message").GetString()
            .Should().Be("Too many requests. Please try again later.");
    }

    [Fact]
    public async Task InvokeAsync_HealthPath_BypassesRateLimiting()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var options = new RateLimitOptions { RequestsPerMinute = 1 };
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, options);
        var firstContext = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"), "/health");
        var secondContext = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"), "/health/ready");

        // Act
        await middleware.InvokeAsync(firstContext);
        await middleware.InvokeAsync(secondContext);

        // Assert
        nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Exactly(2));
        firstContext.Response.Headers.ContainsKey("X-RateLimit-Limit").Should().BeFalse();
        secondContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_DistinctTenantOrIpKeys_UseIndependentBuckets()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var options = new RateLimitOptions { RequestsPerMinute = 1 };
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, options);
        var originalKey = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"));
        var sameKey = CreateContext("tenant-a", IPAddress.Parse("192.0.2.1"));
        var distinctTenant = CreateContext("tenant-b", IPAddress.Parse("192.0.2.1"));
        var distinctIp = CreateContext("tenant-a", IPAddress.Parse("192.0.2.2"));

        // Act
        await middleware.InvokeAsync(originalKey);
        await middleware.InvokeAsync(sameKey);
        await middleware.InvokeAsync(distinctTenant);
        await middleware.InvokeAsync(distinctIp);

        // Assert
        sameKey.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        distinctTenant.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        distinctIp.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Exactly(3));
    }

    private static DefaultHttpContext CreateContext(string tenantId, IPAddress remoteIp, string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = remoteIp;
        context.Items["TenantId"] = tenantId;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
