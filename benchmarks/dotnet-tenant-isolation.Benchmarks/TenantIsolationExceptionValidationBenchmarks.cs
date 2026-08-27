using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using System;
using System.Collections.Generic;
using TenantIsolation.Exceptions;

namespace dotnet_tenant_isolation.Benchmarks;

[MemoryDiagnoser]
public class TenantIsolationExceptionValidationBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    private TenantIsolationException _validException = null!;
    private TenantIsolationException _invalidException = null!;
    private TenantNotActiveException _notActiveException = null!;
    private DataIsolationViolationException _isolationViolation = null!;

    [GlobalSetup]
    public void Setup()
    {
        var details = new Dictionary<string, object?>(N);
        for (int i = 0; i < N; i++)
        {
            details[$"key_{i}"] = $"value_{i}";
        }

        _validException = new TenantIsolationException("Valid tenant error", "VALID_ERROR", details);
        _invalidException = new TenantIsolationException("Invalid tenant error", "", new Dictionary<string, object?>());
        _notActiveException = new TenantNotActiveException(Guid.NewGuid(), "suspended");
        _isolationViolation = new DataIsolationViolationException(Guid.NewGuid(), "Order", "cross-tenant access");
    }

    /// <summary>
    /// Benchmarks validating a well-formed exception with a populated ErrorDetails dictionary.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<string> ValidateValidException()
    {
        return _validException.Validate();
    }

    /// <summary>
    /// Benchmarks validating an exception that fails validation (empty ErrorCode and empty ErrorDetails).
    /// </summary>
    [Benchmark]
    public IReadOnlyList<string> ValidateInvalidException()
    {
        return _invalidException.Validate();
    }

    /// <summary>
    /// Benchmarks the IsValid fast-path check over a valid exception.
    /// </summary>
    [Benchmark]
    public bool IsValidValidException()
    {
        return _validException.IsValid();
    }

    /// <summary>
    /// Benchmarks the derived Validate for TenantNotActiveException, which layers base validation on top.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<string> ValidateNotActiveException()
    {
        return _notActiveException.Validate();
    }

    /// <summary>
    /// Benchmarks the derived Validate for DataIsolationViolationException, which adds TenantId and EntityType checks.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<string> ValidateIsolationViolation()
    {
        return _isolationViolation.Validate();
    }
}