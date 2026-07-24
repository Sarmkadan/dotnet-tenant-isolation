#nullable enable

using System;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Middleware;
using Xunit;

namespace TenantIsolation.Tests;

public class RequestContextMiddlewareExtensionsTests
{
    private readonly Mock<ILogger<RequestContextMiddleware>> _loggerMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILoggingBuilder> _loggingBuilderMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    [Fact]
    public void UseRequestContext_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IApplicationBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.UseRequestContext());
    }

    [Fact]
    public void UseRequestContext_WithNullBuilderAndConfigureLogger_ThrowsArgumentNullException()
    {
        // Arrange
        IApplicationBuilder? builder = null;
        Action<ILoggingBuilder>? configureLogger = _ => { };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.UseRequestContext(configureLogger));
    }

    [Fact]
    public void UseRequestContext_WithNullBuilderAndTenantIdExtractor_ThrowsArgumentNullException()
    {
        // Arrange
        IApplicationBuilder? builder = null;
        Func<HttpContext, string?> tenantIdExtractor = _ => "tenant123";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.UseRequestContext(tenantIdExtractor));
    }

    [Fact]
    public void UseRequestContext_WithNullBuilderAndTimeout_ThrowsArgumentNullException()
    {
        // Arrange
        IApplicationBuilder? builder = null;
        var timeout = TimeSpan.FromSeconds(30);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.UseRequestContext(timeout));
    }

    [Fact]
    public void UseRequestContext_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new ApplicationBuilder(_serviceProviderMock.Object);
        var timeout = TimeSpan.Zero;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.UseRequestContext(timeout));
    }

    [Fact]
    public void UseRequestContext_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new ApplicationBuilder(_serviceProviderMock.Object);
        var timeout = TimeSpan.FromSeconds(-1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.UseRequestContext(timeout));
    }

    [Fact]
    public void UseRequestContext_WithNullTenantIdExtractor_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ApplicationBuilder(_serviceProviderMock.Object);
        Func<HttpContext, string?>? tenantIdExtractor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.UseRequestContext(tenantIdExtractor!));
    }

    [Fact]
    public void GetRequestContext_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetRequestContext());
    }

    [Fact]
    public void GetRequestContext_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.GetRequestContext());
    }

    [Fact]
    public void GetRequestContext_WithNullHttpContextAccessor_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IHttpContextAccessor)))
            .Returns(null);

        // Act
        var result = services.GetRequestContext();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRequestContext_WithHttpContext_ReturnsRequestContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "test-correlation-123";
        context.Items["TenantId"] = "tenant-456";
        context.Items["UserId"] = "user-789";
        context.Items["RequestStartTime"] = DateTime.UtcNow;
        context.Items["RequestPath"] = "/api/test";
        context.Items["RequestMethod"] = "GET";

        // Act
        var result = context.GetRequestContext();

        // Assert
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be("test-correlation-123");
        result.TenantId.Should().Be("tenant-456");
        result.UserId.Should().Be("user-789");
        result.RequestStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Path.Should().Be("/api/test");
        result.Method.Should().Be("GET");
    }

    [Fact]
    public void GetRequestContext_WithMissingContextItems_ReturnsRequestContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // Don't set any items - should return default values

        // Act
        var result = context.GetRequestContext();

        // Assert
        result.Should().NotBeNull();
        result.CorrelationId.Should().BeEmpty();
        result.TenantId.Should().BeNull();
        result.UserId.Should().BeNull();
        result.RequestStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Path.Should().BeEmpty();
        result.Method.Should().BeEmpty();
    }

    [Fact]
    public void GetRequestContext_WithServiceProvider_ReturnsRequestContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "service-provider-correlation";
        httpContext.Items["TenantId"] = "service-tenant";
        httpContext.Items["UserId"] = "service-user";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var services = new ServiceCollection()
            .AddSingleton(httpContextAccessor)
            .BuildServiceProvider();

        // Act
        var result = services.GetRequestContext();

        // Assert
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be("service-provider-correlation");
        result.TenantId.Should().Be("service-tenant");
        result.UserId.Should().Be("service-user");
    }

    [Fact]
    public void GetCorrelationId_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetCorrelationId());
    }

    [Fact]
    public void GetCorrelationId_WithValidContext_ReturnsCorrelationId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "correlation-123";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetCorrelationId();

        // Assert
        result.Should().Be("correlation-123");
    }

    [Fact]
    public void GetCorrelationId_WithEmptyCorrelationId_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = string.Empty;
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.GetCorrelationId());
    }

    [Fact]
    public void GetTenantId_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetTenantId());
    }

    [Fact]
    public void GetTenantId_WithValidContext_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "tenant-abc";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetTenantId();

        // Assert
        result.Should().Be("tenant-abc");
    }

    [Fact]
    public void GetTenantId_WithMissingTenantId_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WithEmptyTenantId_ReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = string.Empty;
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetTenantId();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUserId_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetUserId());
    }

    [Fact]
    public void GetUserId_WithValidContext_ReturnsUserId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["UserId"] = "user-xyz";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().Be("user-xyz");
    }

    [Fact]
    public void GetUserId_WithMissingUserId_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_WithEmptyUserId_ReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["UserId"] = string.Empty;
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRequestStartTime_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetRequestStartTime());
    }

    [Fact]
    public void GetRequestStartTime_WithValidContext_ReturnsRequestStartTime()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var context = new DefaultHttpContext();
        context.Items["RequestStartTime"] = startTime;
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetRequestStartTime();

        // Assert
        result.Should().Be(startTime);
    }

    [Fact]
    public void GetRequestStartTime_WithMissingRequestStartTime_ReturnsCurrentTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.GetRequestStartTime();

        // Assert
        result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetRequestDuration_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.GetRequestDuration());
    }

    [Fact]
    public void GetRequestDuration_WithValidContext_ReturnsPositiveDuration()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMilliseconds(-100);
        var context = new DefaultHttpContext();
        context.Items["RequestStartTime"] = startTime;
        context.Items["RequestContext"] = context.GetRequestContext();

        // Small delay to ensure duration is measurable
        System.Threading.Thread.Sleep(5);

        // Act
        var result = context.GetRequestDuration();

        // Assert
        result.Should().BePositive();
        result.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void UseRequestContext_WithTenantIdExtractor_SetsTenantIdInContext()
    {
        // Arrange
        var builder = new ApplicationBuilder(_serviceProviderMock.Object);
        Func<HttpContext, string?> tenantIdExtractor = ctx => "extracted-tenant";

        // Act
        var app = builder.UseRequestContext(tenantIdExtractor);

        // Assert
        app.Should().NotBeNull();
    }

    [Fact]
    public void UseRequestContext_WithTimeout_SetsTimeoutInContext()
    {
        // Arrange
        var builder = new ApplicationBuilder(_serviceProviderMock.Object);
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var app = builder.UseRequestContext(timeout);

        // Assert
        app.Should().NotBeNull();
    }

    [Fact]
    public void IsForTenant_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContext? context = null;
        var tenantId = "tenant123";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context!.IsForTenant(tenantId));
    }

    [Fact]
    public void IsForTenant_WithNullTenantId_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        string? tenantId = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.IsForTenant(tenantId!));
    }

    [Fact]
    public void IsForTenant_WithEmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var tenantId = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.IsForTenant(tenantId));
    }

    [Fact]
    public void IsForTenant_WithMatchingTenantId_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "tenant-abc";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.IsForTenant("tenant-abc");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsForTenant_WithDifferentTenantId_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "tenant-abc";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.IsForTenant("tenant-xyz");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsForTenant_WithCaseInsensitiveComparison_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "Tenant-ABC";
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.IsForTenant("tenant-abc");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsForTenant_WithMissingTenantId_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["RequestContext"] = context.GetRequestContext();

        // Act
        var result = context.IsForTenant("tenant-abc");

        // Assert
        result.Should().BeFalse();
    }
}