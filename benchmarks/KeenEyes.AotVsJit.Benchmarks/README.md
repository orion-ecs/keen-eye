# KeenEyes AOT vs JIT Performance Benchmarks

This project contains comprehensive benchmarks comparing Native AOT compilation against traditional JIT compilation for KeenEyes.

## Benchmark Categories

### 1. Runtime Performance Benchmarks
- **QueryPerformanceBenchmarks** - Query iteration with 1, 2, and 3 components
- **ComponentAccessBenchmarks** - Get/Set component operations
- **EntityOperationsBenchmarks** - Spawn and despawn operations

### 2. Startup Time Benchmarks
See the separate `StartupTimeBenchmark` directory for measuring cold start performance.

### 3. Memory Usage Analysis
Memory diagnostics are included via `[MemoryDiagnoser]` on all benchmarks.

### 4. Binary Size Comparison
Binary sizes are documented in the results (see `docs/performance/aot-vs-jit-benchmarks.md`).

## Running Benchmarks

### JIT Mode (Default)
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks
dotnet run -c Release
```

### AOT Mode
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks

# Publish with AOT
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT

# Run the published AOT binary
./bin/Release/net10.0/linux-x64/publish/KeenEyes.AotVsJit.Benchmarks
```

### Filtering Benchmarks
```bash
# Run only query benchmarks
dotnet run -c Release -- --filter *Query*

# Run only component access benchmarks
dotnet run -c Release -- --filter *ComponentAccess*

# List all benchmarks
dotnet run -c Release -- --list flat
```

## Platform-Specific Builds

### Windows (x64)
```bash
dotnet publish -c Release -r win-x64 -p:BenchmarkMode=AOT
```

### macOS (ARM64)
```bash
dotnet publish -c Release -r osx-arm64 -p:BenchmarkMode=AOT
```

### Linux (x64)
```bash
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
```

## Expected Results

From issue #326, we expect:

| Metric | JIT | AOT | Expected Difference |
|--------|-----|-----|---------------------|
| Query performance | Baseline | ±5% | Roughly equivalent |
| Component access | Baseline | ±5% | Roughly equivalent |
| Entity spawn | Baseline | ±5% | Roughly equivalent |
| Memory usage | Baseline | -30% | Lower for AOT |

## Notes

- All benchmarks use `[ShortRunJob]` for faster execution
- `[MemoryDiagnoser]` is enabled to track allocations
- Benchmarks are configured to run in Release mode
- AOT mode requires explicit runtime identifier (`-r`)

## Results

See `docs/performance/aot-vs-jit-benchmarks.md` for detailed results and analysis.
