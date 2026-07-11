#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TenantIsolation.Utilities;

/// <summary>
/// Distributed tracing context containing correlation information
/// Enables request tracing across multiple services and logs
/// </summary>
public sealed class TracingContext
{
    /// <summary>
    /// Correlation ID for tracking related requests
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;


    /// <summary>
    /// Trace ID from W3C Trace Context standard
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Span ID for individual operation
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Parent span ID (if in nested operation)
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Request path that initiated the trace
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Tenant context for multi-tenant tracing
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// User context for audit tracing
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Start time of operation
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Additional metadata for debugging
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Initializes a new tracing context with proper defaults
    /// </summary>
    public TracingContext()
    {
        CorrelationId = Guid.NewGuid().ToString("N");
        TraceId = ActivityTraceId.CreateRandom().ToString();
        SpanId = ActivitySpanId.CreateRandom().ToString();
    }

}

/// <summary>
/// Distributed tracing extension methods
/// Provides utilities for logging and managing trace context
/// </summary>
public static class DistributedTracingExtensions
{
    private static readonly AsyncLocal<TracingContext?> CurrentContext = new();

    /// <summary>
    /// Get current tracing context
    /// </summary>
    /// <returns>The current <see cref="TracingContext"/> or null if not set</returns>
    public static TracingContext? GetCurrentContext()
    {
        return CurrentContext.Value;
    }

    /// <summary>
    /// Set current tracing context
    /// </summary>
    /// <param name="context">The tracing context to set</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
    public static void SetCurrentContext(TracingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        CurrentContext.Value = context;
    }

    /// <summary>
    /// Create new tracing context from current or create new
    /// </summary>
    /// <returns>A tracing context, either existing or new</returns>
    public static TracingContext GetOrCreateContext()
    {
        return CurrentContext.Value ?? new TracingContext();
    }

    /// <summary>
    /// Create child context for nested operations
    /// </summary>
    /// <param name="operationName">Optional operation name to track</param>
    /// <returns>A new child tracing context</returns>
    public static TracingContext CreateChildContext(string? operationName = null)
    {
        var parentContext = CurrentContext.Value;
        var childContext = new TracingContext
        {
            CorrelationId = parentContext?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            TraceId = parentContext?.TraceId ?? ActivityTraceId.CreateRandom().ToString(),
            ParentSpanId = parentContext?.SpanId,
            TenantId = parentContext?.TenantId,
            UserId = parentContext?.UserId,
            RequestPath = parentContext?.RequestPath,
            Metadata = new Dictionary<string, string>(parentContext?.Metadata ?? new Dictionary<string, string>())
        };

        if (!string.IsNullOrEmpty(operationName))
        {
            childContext.Metadata["operation"] = operationName;
        }

        return childContext;
    }

    /// <summary>
    /// Create scope that manages tracing context
    /// Useful for dependency injection and nested operations
    /// </summary>
    /// <param name="context">The tracing context to manage</param>
    /// <returns>A disposable scope that restores previous context on dispose</returns>
    public static IDisposable BeginTracingScope(TracingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new TracingScope(context);
    }

    /// <summary>
    /// Add value to tracing metadata
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    public static void AddMetadata(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var context = GetOrCreateContext();
        context.Metadata[key] = value;
        SetCurrentContext(context);
    }

    /// <summary>
    /// Log with tracing context information
    /// Automatically includes correlation ID in logs
    /// </summary>
    /// <typeparam name="T">The logger type</typeparam>
    /// <param name="logger">The logger instance</param>
    /// <param name="logLevel">The log level</param>
    /// <param name="message">The log message</param>
    /// <param name="args">Optional message arguments</param>
    public static void LogWithTracing<T>(
        this ILogger<T> logger,
        LogLevel logLevel,
        string message,
        params object[] args)
    {
        var context = GetCurrentContext();
        if (context != null)
        {
            var tracedMessage = $"[CorrelationId: {context.CorrelationId}] {message}";
            logger.Log(logLevel, tracedMessage, args);
        }
        else
        {
            logger.Log(logLevel, message, args);
        }
    }

    /// <summary>
    /// Measure execution time with tracing
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operationName">Name of the operation being traced</param>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="logger">The logger for tracing output</param>
    /// <returns>The result of the operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when operationName or operation is null</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is empty or whitespace</exception>
    public static async Task<T> ExecuteWithTracingAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(logger);

        var context = CreateChildContext(operationName);
        context.Metadata["operation_start"] = DateTime.UtcNow.ToString("O");

        using (BeginTracingScope(context))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                logger.LogInformation("[Tracing] Operation {OperationName} started. CorrelationId: {CorrelationId}",
                    operationName, context.CorrelationId);

                var result = await operation();

                stopwatch.Stop();
                context.Metadata["operation_duration_ms"] = stopwatch.ElapsedMilliseconds.ToString();

                logger.LogInformation("[Tracing] Operation {OperationName} completed in {Duration}ms. CorrelationId: {CorrelationId}",
                    operationName, stopwatch.ElapsedMilliseconds, context.CorrelationId);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "[Tracing] Operation {OperationName} failed after {Duration}ms. CorrelationId: {CorrelationId}",
                    operationName, stopwatch.ElapsedMilliseconds, context.CorrelationId);
                throw;
            }
        }
    }

    /// <summary>
    /// Get tracing information as structured log state
    /// Useful for logging frameworks
    /// </summary>
    /// <returns>A dictionary containing tracing information</returns>
    public static Dictionary<string, object> GetTracingLogState()
    {
        var context = GetCurrentContext();
        if (context == null)
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            { "CorrelationId", context.CorrelationId },
            { "TraceId", context.TraceId },
            { "SpanId", context.SpanId },
            { "TenantId", context.TenantId?.ToString() ?? "unknown" },
            { "UserId", context.UserId ?? "anonymous" }
        };
    }

    /// <summary>
    /// Tracing scope for managing context lifetime
    /// </summary>
    private sealed class TracingScope : IDisposable
    {
        private readonly TracingContext? _previousContext;

        public TracingScope(TracingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _previousContext = CurrentContext.Value;
            CurrentContext.Value = context;
        }

        public void Dispose()
        {
            CurrentContext.Value = _previousContext;
        }
    }
}

/// <summary>
/// Extension methods for adding distributed tracing to DI
/// </summary>
public static class DistributedTracingServiceExtensions
{
    /// <summary>
    /// Add distributed tracing support
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public static IServiceCollection AddDistributedTracing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        return services;
    }

    /// <summary>
    /// Configure distributed tracing in request pipeline
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The configured application builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when app is null</exception>
    public static IApplicationBuilder UseDistributedTracing(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            var tracingContext = new TracingContext
            {
                RequestPath = context.Request.Path,
                TenantId = context.Items["TenantId"] as Guid?,
                UserId = context.Items["UserId"] as string
            };

            // Try to extract trace context from headers
            if (context.Request.Headers.TryGetValue("X-Trace-Id", out var traceId))
            {
                tracingContext.TraceId = traceId.ToString();
            }

            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                tracingContext.CorrelationId = correlationId.ToString();
            }

            using (DistributedTracingExtensions.BeginTracingScope(tracingContext))
            {
                // Add tracing headers to response
                context.Response.Headers.Add("X-Trace-Id", tracingContext.TraceId);
                context.Response.Headers.Add("X-Correlation-Id", tracingContext.CorrelationId);

                await next(context);
            }
        });
    }
}