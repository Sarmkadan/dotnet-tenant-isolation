#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;

// Run all benchmark classes discovered in this assembly.
// Usage:
//   dotnet run -c Release                  -- interactive switcher
//   dotnet run -c Release -- --filter "*"  -- run everything
BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args);
