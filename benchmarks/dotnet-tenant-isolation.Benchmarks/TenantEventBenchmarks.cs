using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using TenantIsolation.Events;

namespace TenantIsolation.Benchmarks
{
    [MemoryDiagnoser]
    public class TenantEventBenchmarks
    {
        private TenantCreatedEvent _tenantCreatedEvent = null!;
        private TenantResourceAccessedEvent _tenantResourceAccessedEvent = null!;

        [Params(10, 100, 1000)]
        public int Iterations;

        [GlobalSetup]
        public void Setup()
        {
            _tenantCreatedEvent = new TenantCreatedEvent();
            _tenantResourceAccessedEvent = new TenantResourceAccessedEvent();
        }

        [Benchmark]
        public void BenchmarkTenantCreatedEventSetterValidation()
        {
            for (int i = 0; i < Iterations; i++)
            {
                try
                {
                    _tenantCreatedEvent.TenantName = "ValidTenantName";
                    _tenantCreatedEvent.TenantSlug = "valid-tenant-slug";
                    _tenantCreatedEvent.AdminEmail = "admin@example.com";
                    _tenantCreatedEvent.IsolationStrategy = "Shared";
                }
                catch (ArgumentException)
                {
                    // Ignore validation exceptions for benchmarking purposes
                }
            }
        }

        [Benchmark]
        public void BenchmarkTenantResourceAccessedEventSetterValidation()
        {
            for (int i = 0; i < Iterations; i++)
            {
                try
                {
                    _tenantResourceAccessedEvent.ResourceType = "Database";
                    _tenantResourceAccessedEvent.ResourceId = "resource-id-123";
                    _tenantResourceAccessedEvent.Action = "Read";
                }
                catch (ArgumentException)
                {
                    // Ignore validation exceptions for benchmarking purposes
                }
            }
        }
    }
}
