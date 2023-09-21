#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Benchmarks;

/// <summary>
/// Benchmarks for tenant resolution operations.
/// Measures the performance of tenant identification strategies.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class TenantResolutionBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private TenantResolutionService? _resolutionService;
    private IServiceScope? _scope;
    private HttpContext? _httpContext;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddHttpContextAccessor();

        // Setup in-memory tenant store with some tenants
        services.AddSingleton<IDynamicTenantStore>(new InMemoryTenantStore(new List<Tenant>
        {
            new Tenant { Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"), Name = "Acme Corp", Slug = "acme-corp", Status = TenantStatus.Active },
            new Tenant { Id = Guid.Parse("a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8"), Name = "Globex Corp", Slug = "globex-corp", Status = TenantStatus.Active },
            new Tenant { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), Name = "Test Tenant", Slug = "test-tenant", Status = TenantStatus.Active }
        }));

        services.AddTenantIsolationInMemory("BenchmarkDb", options =>
        {
            options.AutoMigrate = true;
            options.EnableAuditLogging = false;
        });

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _resolutionService = _scope.ServiceProvider.GetRequiredService<TenantResolutionService>();

        // Setup HTTP context with tenant header
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
        _httpContext = httpContext;
    }

    /// <summary>
    /// Baseline: Resolve tenant from HTTP header (X-Tenant-Id).
    /// This is the most common path in production.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async ValueTask<Tenant> ResolveTenant_FromHeader()
    {
        if (_httpContext != null)
        {
            var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = _httpContext;
        }
        return await _resolutionService!.ResolveTenantAsync();
    }

    /// <summary>
    /// Resolve tenant from route parameter.
    /// Common in RESTful APIs.
    /// </summary>
    [Benchmark]
    public async ValueTask<Tenant> ResolveTenant_FromRoute()
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "f47ac10b-58cc-4372-a567-0e02b2c3d479";
        var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        return await _resolutionService!.ResolveTenantAsync();
    }

    /// <summary>
    /// Resolve tenant from user claims.
    /// Common in authenticated scenarios.
    /// </summary>
    [Benchmark]
    public async ValueTask<Tenant> ResolveTenant_FromClaims()
    {
        var context = new DefaultHttpContext();
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("tenant_id", "f47ac10b-58cc-4372-a567-0e02b2c3d479"),
            new System.Security.Claims.Claim("tenant_slug", "acme-corp")
        }));
        var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        return await _resolutionService!.ResolveTenantAsync();
    }

    /// <summary>
    /// Resolve tenant from subdomain.
    /// Common in SaaS applications.
    /// </summary>
    [Benchmark]
    public async ValueTask<Tenant> ResolveTenant_FromSubdomain()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme-corp.example.com");
        var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        return await _resolutionService!.ResolveTenantAsync();
    }

    /// <summary>
    /// Get currently resolved tenant (cache hit scenario).
    /// This tests the fast path after tenant has been resolved.
    /// </summary>
    [Benchmark]
    public Tenant GetCurrentTenant()
    {
        // Simulate that tenant was already resolved and stored in context
        var context = new DefaultHttpContext();
        var tenant = new Tenant { Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"), Name = "Acme Corp", Slug = "acme-corp", Status = TenantStatus.Active };
        context.Items[TenantIsolation.Constants.TenantConstants.CurrentTenantContextKey] = tenant;
        var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        return _resolutionService!.GetCurrentTenant();
    }

    /// <summary>
    /// Check if tenant exists (HasTenant).
    /// Very lightweight operation.
    /// </summary>
    [Benchmark]
    public bool HasTenant()
    {
        var context = new DefaultHttpContext();
        var tenant = new Tenant { Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"), Name = "Acme Corp", Slug = "acme-corp", Status = TenantStatus.Active };
        context.Items[TenantIsolation.Constants.TenantConstants.CurrentTenantContextKey] = tenant;
        var accessor = _scope!.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;
        return _resolutionService!.HasTenant();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    public void Dispose()
    {
        Cleanup();
    }
}

/// <summary>
/// In-memory implementation of IDynamicTenantStore for benchmarking.
/// </summary>
internal sealed class InMemoryTenantStore : IDynamicTenantStore
{
    private readonly List<Tenant> _tenants;
    private readonly Dictionary<Guid, Tenant> _tenantDict;
    private readonly Dictionary<string, Tenant> _tenantSlugDict;

    public event EventHandler<TenantEventArgs>? OnTenantRegistered;
    public event EventHandler<TenantEventArgs>? OnTenantRemoved;

    public InMemoryTenantStore(List<Tenant> tenants)
    {
        _tenants = tenants;
        _tenantDict = tenants.ToDictionary(t => t.Id);
        _tenantSlugDict = tenants.ToDictionary(t => t.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public Task<Tenant?> GetTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tenantDict.TryGetValue(id, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        _tenantSlugDict.TryGetValue(slug, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<IEnumerable<Tenant>> GetAllActiveTenantsAsync()
    {
        return Task.FromResult<IEnumerable<Tenant>>(_tenants.Where(t => t.Status == TenantStatus.Active));
    }

    public Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        _tenantDict.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<bool> TenantExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tenantDict.ContainsKey(id));
    }

    public Task<bool> TenantExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tenantSlugDict.ContainsKey(slug));
    }

    public Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants.Add(tenant);
        _tenantDict[tenant.Id] = tenant;
        _tenantSlugDict[tenant.Slug] = tenant;
        OnTenantRegistered?.Invoke(this, new TenantEventArgs { Tenant = tenant });
        return Task.CompletedTask;
    }

    public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (_tenantDict.TryGetValue(tenant.Id, out var existing))
        {
            _tenants.Remove(existing);
            _tenants.Add(tenant);
            _tenantDict[tenant.Id] = tenant;
            _tenantSlugDict[tenant.Slug] = tenant;
        }
        return Task.CompletedTask;
    }

    public Task DeleteTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenantDict.TryGetValue(id, out var tenant))
        {
            _tenants.Remove(tenant);
            _tenantDict.Remove(id);
            _tenantSlugDict.Remove(tenant.Slug);
            OnTenantRemoved?.Invoke(this, new TenantEventArgs { Tenant = tenant });
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}