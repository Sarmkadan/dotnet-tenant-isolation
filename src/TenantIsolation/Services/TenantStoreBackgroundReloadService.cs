#nullable enable

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace TenantIsolation.Services;

/// <summary>
/// A background hosted service to manage the lifecycle of the DynamicTenantStore's
/// periodic reloading mechanism.
/// </summary>
public class TenantStoreBackgroundReloadService : IHostedService, IAsyncDisposable
{
    private readonly IDynamicTenantStore _dynamicTenantStore;
    private readonly ILogger<TenantStoreBackgroundReloadService> _logger;

    public TenantStoreBackgroundReloadService(
        IDynamicTenantStore dynamicTenantStore,
        ILogger<TenantStoreBackgroundReloadService> logger)
    {
        _dynamicTenantStore = dynamicTenantStore;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TenantStoreBackgroundReloadService starting.");
        if (_dynamicTenantStore is DynamicTenantStore concreteStore)
        {
            concreteStore.StartReloading();
        }
        else
        {
            _logger.LogWarning("IDynamicTenantStore implementation is not DynamicTenantStore. Cannot start reloading directly.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TenantStoreBackgroundReloadService stopping.");
        if (_dynamicTenantStore is DynamicTenantStore concreteStore)
        {
            concreteStore.StopReloading();
        }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_dynamicTenantStore is DynamicTenantStore concreteStore)
        {
            concreteStore.Dispose();
        }
        await ValueTask.CompletedTask;
    }
}
