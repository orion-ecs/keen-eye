using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using KeenEyes.Benchmarks;

// Run all benchmarks
// Usage:
//   dotnet run -c Release                     # Run all benchmarks
//   dotnet run -c Release -- --filter *Query* # Run only query benchmarks
//   dotnet run -c Release -- --list flat      # List all benchmarks

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher.FromAssembly(typeof(EntityBenchmarks).Assembly).Run(args, config);
