using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using System;
using System.Collections.Generic;
using TenantIsolation.Integration;

namespace dotnet_tenant_isolation.Benchmarks;

[MemoryDiagnoser]
public class ApiCallResultBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    private string? _sampleData;
    private string? _errorMessage;

    [GlobalSetup]
    public void Setup()
    {
        // Prepare realistic test data
        _sampleData = new string('A', 100); // Simulate a typical JSON response payload
        _errorMessage = "An error occurred while processing the external request.";
    }

    /// <summary>
    /// Benchmarks the instantiation of a successful API result.
    /// </summary>
    [Benchmark]
    public ApiCallResult<string> CreateSuccessResult()
    {
        return new ApiCallResult<string>
        {
            IsSuccess = true,
            Data = _sampleData,
            HttpStatusCode = 200,
            Duration = TimeSpan.FromMilliseconds(50)
        };
    }

    /// <summary>
    /// Benchmarks the instantiation of a failed API result.
    /// </summary>
    [Benchmark]
    public ApiCallResult<string> CreateErrorResult()
    {
        return new ApiCallResult<string>
        {
            IsSuccess = false,
            ErrorMessage = _errorMessage,
            HttpStatusCode = 500,
            Duration = TimeSpan.FromMilliseconds(10)
        };
    }

    /// <summary>
    /// Benchmarks the creation of a list of results to simulate batch processing.
    /// This utilizes the [Params] N property.
    /// </summary>
    [Benchmark]
    public List<ApiCallResult<string>> CreateResultList()
    {
        var list = new List<ApiCallResult<string>>(N);
        for (int i = 0; i < N; i++)
        {
            list.Add(new ApiCallResult<string>
            {
                IsSuccess = i % 2 == 0,
                Data = i % 2 == 0 ? _sampleData : null,
                ErrorMessage = i % 2 != 0 ? _errorMessage : null,
                HttpStatusCode = i % 2 == 0 ? 200 : 500,
                Duration = TimeSpan.FromMilliseconds(i)
            });
        }
        return list;
    }
}
