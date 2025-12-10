using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using KeenEyes.Spatial.Benchmarks;

// Run spatial partitioning benchmarks
// Usage:
//   dotnet run -c Release                        # Run all spatial benchmarks
//   dotnet run -c Release -- --filter *Radius*   # Run only radius query benchmarks
//   dotnet run -c Release -- --list flat         # List all benchmarks

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher.FromAssembly(typeof(SpatialBenchmarks).Assembly).Run(args, config);
