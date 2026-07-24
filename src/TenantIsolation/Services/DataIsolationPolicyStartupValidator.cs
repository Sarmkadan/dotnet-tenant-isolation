#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TenantIsolation.Events;
using TenantIsolation.Models;

namespace TenantIsolation.Services;

/// <summary>
/// Startup guardrail that validates every configured <see cref="DataIsolationPolicy"/> at
/// boot via <see cref="IDataIsolationPolicyValidator.ValidateAllAsync"/> and fails application
/// startup with an aggregated report when any policy is misconfigured, instead of letting the
/// problem surface later as a cross-tenant data leak. Also re-runs validation whenever a
/// <see cref="DataIsolationPolicyChangedEvent"/> is published, logging (but not crashing on)
/// any regression introduced after startup.
/// </summary>
public sealed class DataIsolationPolicyStartupValidator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DataIsolationPolicyStartupValidator> _logger;
    private Func<DataIsolationPolicyChangedEvent, Task>? _subscribedHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIsolationPolicyStartupValidator"/> class.
    /// </summary>
    /// <param name="scopeFactory">Used to create a scope for resolving the scoped <see cref="IDataIsolationPolicyValidator"/>.</param>
    /// <param name="eventBus">Event bus subscribed to for policy-change re-validation.</param>
    /// <param name="logger">Logger used to record validation outcomes.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public DataIsolationPolicyStartupValidator(
        IServiceScopeFactory scopeFactory,
        IEventBus eventBus,
        ILogger<DataIsolationPolicyStartupValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Validates all data isolation policies and throws with an aggregated report if any are
    /// invalid, then subscribes to <see cref="DataIsolationPolicyChangedEvent"/> for ongoing
    /// re-validation.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while validating.</param>
    /// <returns>A task that completes once startup validation and subscription succeed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if one or more policies fail validation.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var report = await RunValidationAsync(cancellationToken);

        if (!report.IsValid)
        {
            var message = report.ToDisplayString();
            _logger.LogCritical("Startup data isolation policy validation failed:{NewLine}{Report}", Environment.NewLine, message);
            throw new InvalidOperationException("Startup data isolation policy validation failed:" + Environment.NewLine + message);
        }

        _subscribedHandler = OnPolicyChangedAsync;
        _eventBus.Subscribe(_subscribedHandler);
    }

    /// <summary>
    /// Unsubscribes from policy-change notifications on shutdown.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while stopping.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscribedHandler is not null)
        {
            _eventBus.Unsubscribe(_subscribedHandler);
            _subscribedHandler = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-runs full policy validation in response to a policy change, logging an aggregated
    /// report if the change introduced a misconfiguration. Does not throw, since the
    /// application is already running and fail-fast is only appropriate at startup.
    /// </summary>
    /// <param name="changeEvent">The change notification that triggered re-validation.</param>
    /// <returns>A task that completes once re-validation has run.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="changeEvent"/> is null.</exception>
    private async Task OnPolicyChangedAsync(DataIsolationPolicyChangedEvent changeEvent)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);

        var report = await RunValidationAsync(CancellationToken.None);

        if (report.IsValid)
        {
            _logger.LogInformation(
                "Re-validated data isolation policies after change to {PolicyType} (tenant {TenantId}): all valid.",
                changeEvent.PolicyType, changeEvent.TenantId);
        }
        else
        {
            _logger.LogError(
                "Re-validation after change to {PolicyType} (tenant {TenantId}) found misconfigured policies:{NewLine}{Report}",
                changeEvent.PolicyType, changeEvent.TenantId, Environment.NewLine, report.ToDisplayString());
        }
    }

    /// <summary>
    /// Resolves a scoped <see cref="IDataIsolationPolicyValidator"/> and runs a full validation pass.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while validating.</param>
    /// <returns>The aggregated validation report.</returns>
    private async Task<PolicyValidationReport> RunValidationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDataIsolationPolicyValidator>();
        return await validator.ValidateAllAsync(cancellationToken);
    }
}
