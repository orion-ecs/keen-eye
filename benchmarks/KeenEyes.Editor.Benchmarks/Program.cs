using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

using KeenEyes.Editor.Benchmarks;

// Editor entity-scale benchmarks (issue #1005).
// Measures the perf-relevant, headless-testable editor operations against
// deterministically generated scenes of 1,000 and 5,000 entities.
//
// Usage:
//   dotnet run -c Release                              # Run all editor benchmarks
//   dotnet run -c Release -- --filter *PlayMode*       # Run only play-mode benchmarks
//   dotnet run -c Release -- --list flat               # List all benchmarks

// The benchmark project transitively references the whole editor + engine graph. Under the
// default toolchain, BenchmarkDotNet spins up a fresh isolated build of that entire graph per
// run, which blows past its 2-minute build timeout. Running in-process (emit toolchain) skips
// the isolated build entirely - appropriate for a baseline macro-benchmark where absolute
// isolation matters less than getting stable millisecond-scale numbers. Paired with a ShortRun
// job (3 warmup + 3 target iterations). MemoryDiagnoser is applied per class.
var job = Job.ShortRun
    .WithToolchain(InProcessEmitToolchain.Instance);

var config = DefaultConfig.Instance
    .AddJob(job)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher
    .FromAssembly(typeof(SceneGenerator).Assembly)
    .Run(args, config);
