using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using KeenEyes.AotVsJit.Benchmarks;

// AOT vs JIT Runtime Performance Benchmarks
//
// Usage:
//   JIT mode:
//     dotnet run -c Release
//
//   AOT mode:
//     dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
//     ./bin/Release/net10.0/linux-x64/publish/KeenEyes.AotVsJit.Benchmarks
//
//   Filter benchmarks:
//     dotnet run -c Release -- --filter *Query*
//
//   List benchmarks:
//     dotnet run -c Release -- --list flat

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher.FromAssembly(typeof(QueryPerformanceBenchmarks).Assembly).Run(args, config);
