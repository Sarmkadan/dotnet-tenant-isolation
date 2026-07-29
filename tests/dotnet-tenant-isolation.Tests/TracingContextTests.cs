using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Utilities;

namespace dotnet_tenant_isolation.Tests
{
    public class TracingContextTests
    {
        [Fact]
        public void TracingContext_Constructor_InitializesWithDefaults()
        {
            // Act
            var context = new TracingContext();

            // Assert
            Assert.False(string.IsNullOrEmpty(context.CorrelationId));
            Assert.False(string.IsNullOrEmpty(context.TraceId));
            Assert.False(string.IsNullOrEmpty(context.SpanId));
            Assert.Equal(DateTimeKind.Utc, context.StartTime.Kind);
            Assert.Null(context.ParentSpanId);
            Assert.Null(context.RequestPath);
            Assert.Null(context.TenantId);
            Assert.Null(context.UserId);
            Assert.NotNull(context.Metadata);
            Assert.Empty(context.Metadata);
        }

        [Fact]
        public void TracingContext_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var context = new TracingContext();
            var testGuid = Guid.NewGuid();
            var testDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var testMetadata = new Dictionary<string, string> { { "key", "value" } };

            // Act
            context.CorrelationId = "test-correlation";
            context.TraceId = "test-trace";
            context.SpanId = "test-span";
            context.ParentSpanId = "parent-span";
            context.RequestPath = "/test/path";
            context.TenantId = testGuid;
            context.UserId = "test-user";
            context.StartTime = testDate;
            context.Metadata = testMetadata;

            // Assert
            Assert.Equal("test-correlation", context.CorrelationId);
            Assert.Equal("test-trace", context.TraceId);
            Assert.Equal("test-span", context.SpanId);
            Assert.Equal("parent-span", context.ParentSpanId);
            Assert.Equal("/test/path", context.RequestPath);
            Assert.Equal(testGuid, context.TenantId);
            Assert.Equal("test-user", context.UserId);
            Assert.Equal(testDate, context.StartTime);
            Assert.Equal(testMetadata, context.Metadata);
        }
    }

    public class DistributedTracingExtensionsTests
    {
        [Fact]
        public void GetCurrentContext_ReturnsNull_WhenNotSet()
        {
            // Act
            var context = DistributedTracingExtensions.GetCurrentContext();

            // Assert
            Assert.Null(context);
        }

        [Fact]
        public void SetCurrentContext_ThrowsArgumentNullException_WhenContextIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DistributedTracingExtensions.SetCurrentContext(null));
        }

        [Fact]
        public void SetCurrentContext_GetCurrentContext_ReturnsSameContext()
        {
            // Arrange
            var context = new TracingContext { CorrelationId = "test" };

            // Act
            DistributedTracingExtensions.SetCurrentContext(context);
            var retrieved = DistributedTracingExtensions.GetCurrentContext();

            // Assert
            Assert.Same(context, retrieved);
            Assert.Equal("test", retrieved?.CorrelationId);
        }

        [Fact]
        public void GetOrCreateContext_ReturnsNewContext_WhenNoneSet()
        {
            // Act
            var context = DistributedTracingExtensions.GetOrCreateContext();

            // Assert
            Assert.NotNull(context);
            Assert.False(string.IsNullOrEmpty(context.CorrelationId));
        }

        [Fact]
        public void GetOrCreateContext_ReturnsExistingContext_WhenSet()
        {
            // Arrange
            var existingContext = new TracingContext { CorrelationId = "existing" };
            DistributedTracingExtensions.SetCurrentContext(existingContext);

            // Act
            var context = DistributedTracingExtensions.GetOrCreateContext();

            // Assert
            Assert.Same(existingContext, context);
            Assert.Equal("existing", context.CorrelationId);
        }

        [Fact]
        public void CreateChildContext_CreatesContextWithInheritedValues()
        {
            // Arrange
            var parentContext = new TracingContext
            {
                CorrelationId = "parent-correlation",
                TraceId = "parent-trace",
                SpanId = "parent-span",
                TenantId = Guid.NewGuid(),
                UserId = "parent-user",
                RequestPath = "/parent"
            };
            parentContext.Metadata.Add("parent-key", "parent-value");
            DistributedTracingExtensions.SetCurrentContext(parentContext);

            // Act
            var childContext = DistributedTracingExtensions.CreateChildContext("child-operation");

            // Assert
            Assert.NotNull(childContext);
            Assert.Equal(parentContext.CorrelationId, childContext.CorrelationId);
            Assert.Equal(parentContext.TraceId, childContext.TraceId);
            Assert.Equal(parentContext.SpanId, childContext.ParentSpanId);
            Assert.Equal(parentContext.TenantId, childContext.TenantId);
            Assert.Equal(parentContext.UserId, childContext.UserId);
            Assert.Equal(parentContext.RequestPath, childContext.RequestPath);
            Assert.Contains("operation", childContext.Metadata.Keys);
            Assert.Equal("child-operation", childContext.Metadata["operation"]);
            Assert.Contains("parent-key", childContext.Metadata.Keys);
            Assert.Equal("parent-value", childContext.Metadata["parent-key"]);
        }

        [Fact]
        public void CreateChildContext_CreatesContextWithNewValues_WhenNoParentContext()
        {
            // Act
            var childContext = DistributedTracingExtensions.CreateChildContext("child-operation");

            // Assert
            Assert.NotNull(childContext);
            Assert.False(string.IsNullOrEmpty(childContext.CorrelationId));
            Assert.False(string.IsNullOrEmpty(childContext.TraceId));
            Assert.False(string.IsNullOrEmpty(childContext.SpanId));
            Assert.Null(childContext.ParentSpanId);
            Assert.Contains("operation", childContext.Metadata.Keys);
            Assert.Equal("child-operation", childContext.Metadata["operation"]);
        }

        [Fact]
        public void BeginTracingScope_ReturnsDisposable_ThatRestoresPreviousContext()
        {
            // Arrange
            var parentContext = new TracingContext { CorrelationId = "parent" };
            DistributedTracingExtensions.SetCurrentContext(parentContext);
            var childContext = new TracingContext { CorrelationId = "child" };

            // Act
            using (DistributedTracingExtensions.BeginTracingScope(childContext))
            {
                var current = DistributedTracingExtensions.GetCurrentContext();
                Assert.Same(childContext, current);
            }

            // Assert
            var restored = DistributedTracingExtensions.GetCurrentContext();
            Assert.Same(parentContext, restored);
        }

        [Fact]
        public void AddMetadata_ThrowsArgumentNullException_WhenKeyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DistributedTracingExtensions.AddMetadata(null, "value"));
        }

        [Fact]
        public void AddMetadata_ThrowsArgumentException_WhenKeyIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => DistributedTracingExtensions.AddMetadata(string.Empty, "value"));
        }

        [Fact]
        public void AddMetadata_AddsValueToContextMetadata()
        {
            // Act
            DistributedTracingExtensions.AddMetadata("test-key", "test-value");

            // Assert
            var context = DistributedTracingExtensions.GetOrCreateContext();
            Assert.NotNull(context);
            Assert.Contains("test-key", context.Metadata.Keys);
            Assert.Equal("test-value", context.Metadata["test-key"]);
        }

        [Fact]
        public void LogWithTracing_IncludesCorrelationId_WhenContextIsSet()
        {
            // Arrange
            var context = new TracingContext { CorrelationId = "test-correlation" };
            DistributedTracingExtensions.SetCurrentContext(context);
            var loggerMock = new Mock<ILogger<object>>();

            // Act
            DistributedTracingExtensions.LogWithTracing(loggerMock.Object, LogLevel.Information, "Test message");

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[CorrelationId: test-correlation]")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void LogWithTracing_LogsNormally_WhenNoContextIsSet()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<object>>();

            // Act
            DistributedTracingExtensions.LogWithTracing(loggerMock.Object, LogLevel.Warning, "Test message {Param}", 42);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == "Test message 42"),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteWithTracingAsync_ExecutesOperationAndLogsStartAndEnd()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            var operationResult = "test-result";
            var operationMock = new Mock<Func<Task<string>>>();
            operationMock.Setup(m => m()).ReturnsAsync(operationResult);

            // Act
            var result = await DistributedTracingExtensions.ExecuteWithTracingAsync(
                "test-operation",
                operationMock.Object,
                loggerMock.Object);

            // Assert
            Assert.Equal(operationResult, result);
            operationMock.Verify(m => m(), Times.Once);

            // Verify logging
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[Tracing] Operation test-operation started")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[Tracing] Operation test-operation completed")),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteWithTracingAsync_LogsError_WhenOperationThrows()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            var testException = new InvalidOperationException("test error");
            var operationMock = new Mock<Func<Task<string>>>();
            operationMock.Setup(m => m()).ThrowsAsync(testException);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                DistributedTracingExtensions.ExecuteWithTracingAsync(
                    "test-operation",
                    operationMock.Object,
                    loggerMock.Object));

            // Assert
            operationMock.Verify(m => m(), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[Tracing] Operation test-operation failed")),
                    testException,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void GetTracingLogState_ReturnsEmptyDictionary_WhenNoContext()
        {
            // Act
            var state = DistributedTracingExtensions.GetTracingLogState();

            // Assert
            Assert.NotNull(state);
            Assert.Empty(state);
        }

        [Fact]
        public void GetTracingLogState_ReturnsTracingInformation_WhenContextIsSet()
        {
            // Arrange
            var context = new TracingContext
            {
                CorrelationId = "test-correlation",
                TraceId = "test-trace",
                SpanId = "test-span",
                TenantId = Guid.NewGuid(),
                UserId = "test-user"
            };
            DistributedTracingExtensions.SetCurrentContext(context);

            // Act
            var state = DistributedTracingExtensions.GetTracingLogState();

            // Assert
            Assert.NotNull(state);
            Assert.Equal("test-correlation", state["CorrelationId"]);
            Assert.Equal("test-trace", state["TraceId"]);
            Assert.Equal("test-span", state["SpanId"]);
            Assert.Equal(context.TenantId.ToString(), state["TenantId"]);
            Assert.Equal("test-user", state["UserId"]);
        }
    }
}