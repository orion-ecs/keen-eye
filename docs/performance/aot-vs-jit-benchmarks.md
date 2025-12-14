# AOT vs JIT Performance Benchmarks

This document presents comprehensive performance benchmarking results comparing Native AOT compilation against traditional JIT compilation for KeenEyes.

## Methodology

### Test Environment
- **Platform**: Linux x64 (GitHub Actions runner)
- **Runtime**: .NET 10.0
- **CPU**: GitHub Actions standard runner (2-core)
- **Memory**: 7 GB RAM
- **Benchmark Tool**: BenchmarkDotNet 0.15.8
- **Configuration**: Release mode, optimizations enabled

### Benchmark Categories

1. **Runtime Performance**
   - Query iteration (1, 2, 3 components)
   - Component access (Get, Set, multiple components)
   - Entity operations (Spawn, Despawn)

2. **Startup Time**
   - Cold start from process launch to first ECS operation
   - Measured over 100 runs for statistical significance

3. **Memory Usage**
   - Process working set
   - GC heap allocations
   - Tracked via BenchmarkDotNet's MemoryDiagnoser

4. **Binary Size**
   - Self-contained publish size
   - Framework-dependent publish size (JIT only)
   - Comparison across platforms

## Expected vs Actual Results

From issue #326, the expected performance characteristics were:

| Metric | JIT | AOT | Expected Difference |
|--------|-----|-----|---------------------|
| Startup time | 200ms | 50ms | 4x faster |
| Memory usage | 50MB | 35MB | 30% less |
| Binary size (self-contained) | 80MB | 25MB | 68% smaller |
| Query performance | Baseline | ±5% | Roughly equivalent |
| Component access | Baseline | ±5% | Roughly equivalent |

## Runtime Performance Benchmarks

### Query Iteration Performance

**Test Setup**: Entities with various component combinations (Position, Velocity, Health)

#### QueryPerformanceBenchmarks

**Entity Count: 1,000**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| QuerySingleComponent | JIT | - | - | - |
| QuerySingleComponent | AOT | - | - | - |
| QueryTwoComponents | JIT | - | - | - |
| QueryTwoComponents | AOT | - | - | - |
| QueryThreeComponents | JIT | - | - | - |
| QueryThreeComponents | AOT | - | - | - |

**Entity Count: 10,000**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| QuerySingleComponent | JIT | - | - | - |
| QuerySingleComponent | AOT | - | - | - |
| QueryTwoComponents | JIT | - | - | - |
| QueryTwoComponents | AOT | - | - | - |
| QueryThreeComponents | JIT | - | - | - |
| QueryThreeComponents | AOT | - | - | - |

**Analysis**: _(To be filled with actual results)_

### Component Access Performance

**Test Setup**: Direct component Get/Set operations on entity arrays

#### ComponentAccessBenchmarks

**Entity Count: 100**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| GetComponent | JIT | - | - | - |
| GetComponent | AOT | - | - | - |
| ModifyComponent | JIT | - | - | - |
| ModifyComponent | AOT | - | - | - |
| GetMultipleComponents | JIT | - | - | - |
| GetMultipleComponents | AOT | - | - | - |

**Entity Count: 1,000**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| GetComponent | JIT | - | - | - |
| GetComponent | AOT | - | - | - |
| ModifyComponent | JIT | - | - | - |
| ModifyComponent | AOT | - | - | - |
| GetMultipleComponents | JIT | - | - | - |
| GetMultipleComponents | AOT | - | - | - |

**Analysis**: _(To be filled with actual results)_

### Entity Operations Performance

**Test Setup**: Spawn and despawn operations with various component counts

#### EntityOperationsBenchmarks

**Entity Count: 100**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| SpawnEntities | JIT | - | - | - |
| SpawnEntities | AOT | - | - | - |
| SpawnAndDespawnEntities | JIT | - | - | - |
| SpawnAndDespawnEntities | AOT | - | - | - |
| SpawnEntitiesWithManyComponents | JIT | - | - | - |
| SpawnEntitiesWithManyComponents | AOT | - | - | - |

**Entity Count: 1,000**

| Benchmark | Mode | Mean | StdDev | Allocated |
|-----------|------|------|--------|-----------|
| SpawnEntities | JIT | - | - | - |
| SpawnEntities | AOT | - | - | - |
| SpawnAndDespawnEntities | JIT | - | - | - |
| SpawnAndDespawnEntities | AOT | - | - | - |
| SpawnEntitiesWithManyComponents | JIT | - | - | - |
| SpawnEntitiesWithManyComponents | AOT | - | - | - |

**Analysis**: _(To be filled with actual results)_

## Startup Time Benchmarks

**Test Setup**: Minimal world with one system, one entity, one update cycle

### Results (100 runs)

| Metric | JIT | AOT | Difference |
|--------|-----|-----|------------|
| Median | - ms | - ms | - |
| P95 | - ms | - ms | - |
| P99 | - ms | - ms | - |
| Mean | - ms | - ms | - |
| Min | - ms | - ms | - |
| Max | - ms | - ms | - |

**Analysis**: _(To be filled with actual results)_

## Memory Usage Analysis

**Test Setup**: Spawn 100,000 entities with 3 components each, run 100 update cycles

| Metric | JIT | AOT | Difference |
|--------|-----|-----|------------|
| Process Working Set | - MB | - MB | - |
| GC Heap Size | - MB | - MB | - |
| Total Allocations | - MB | - MB | - |
| Peak Memory | - MB | - MB | - |

**Analysis**: _(To be filled with actual results)_

## Binary Size Comparison

### Linux x64

| Configuration | JIT | AOT | Difference |
|---------------|-----|-----|------------|
| Self-contained | - MB | - MB | - |
| Framework-dependent | - MB | N/A | - |
| Trimmed self-contained | - MB | - MB | - |

### Windows x64

| Configuration | JIT | AOT | Difference |
|---------------|-----|-----|------------|
| Self-contained | - MB | - MB | - |
| Framework-dependent | - MB | N/A | - |
| Trimmed self-contained | - MB | - MB | - |

### macOS ARM64

| Configuration | JIT | AOT | Difference |
|---------------|-----|-----|------------|
| Self-contained | - MB | - MB | - |
| Framework-dependent | - MB | N/A | - |
| Trimmed self-contained | - MB | - MB | - |

**Analysis**: _(To be filled with actual results)_

## Platform-Specific Results

### Linux x64
- Benchmarks run on GitHub Actions standard runner
- See tables above for detailed results

### Windows x64
- _(Results to be run on Windows CI or local machine)_

### macOS ARM64
- _(Results to be run on macOS CI or local machine)_

## Key Findings

### Runtime Performance
- _(To be filled with analysis after running benchmarks)_
- Query iteration: _(expected ±5% difference)_
- Component access: _(expected ±5% difference)_
- Entity operations: _(expected ±5% difference)_

### Startup Time
- _(To be filled with analysis after running benchmarks)_
- Expected: 4x faster startup for AOT
- Actual: _(to be measured)_

### Memory Usage
- _(To be filled with analysis after running benchmarks)_
- Expected: 30% less memory for AOT
- Actual: _(to be measured)_

### Binary Size
- _(To be filled with analysis after running benchmarks)_
- Expected: 68% smaller for AOT
- Actual: _(to be measured)_

## Recommendations

### When to Use JIT
- _(To be filled based on benchmark results)_
- Development and debugging (faster build times)
- Dynamic plugin loading scenarios
- Platforms without AOT support

### When to Use AOT
- _(To be filled based on benchmark results)_
- Production deployments (faster startup, smaller size)
- Resource-constrained environments (lower memory)
- Security-sensitive scenarios (no dynamic code generation)
- Serverless/container environments (smaller images)

## Running the Benchmarks

### Runtime Performance Benchmarks

**JIT Mode:**
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks
dotnet run -c Release
```

**AOT Mode:**
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
./bin/Release/net10.0/linux-x64/publish/KeenEyes.AotVsJit.Benchmarks
```

### Startup Time Benchmarks

**JIT Mode (100 runs):**
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks/StartupTimeBenchmark
for i in {1..100}; do
  dotnet run -c Release 2>&1 | grep "Startup time"
done > jit-results.txt
```

**AOT Mode (100 runs):**
```bash
cd benchmarks/KeenEyes.AotVsJit.Benchmarks/StartupTimeBenchmark
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
for i in {1..100}; do
  ./bin/Release/net10.0/linux-x64/publish/StartupTimeBenchmark 2>&1 | grep "Startup time"
done > aot-results.txt
```

### Binary Size Measurement

**JIT Mode:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained
du -sh bin/Release/net10.0/linux-x64/publish/
```

**AOT Mode:**
```bash
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
du -sh bin/Release/net10.0/linux-x64/publish/
```

## Limitations

### Current Benchmarks
- Only run on Linux x64 in GitHub Actions environment
- Windows and macOS results require manual testing on those platforms
- Benchmark duration limited to avoid long CI runs (using ShortRunJob)

### Future Work
- Run benchmarks on additional platforms (Windows, macOS)
- Add long-running benchmarks for more statistical accuracy
- Measure performance under concurrent load
- Profile memory allocations in detail
- Add benchmarks for serialization and other subsystems

## Related Documentation

- [Native AOT Deployment Guide](../aot-deployment.md)
- [Issue #326 - Performance Benchmarking](https://github.com/orion-ecs/keen-eye/issues/326)
- [Issue #81 - Native AOT Compatibility](https://github.com/orion-ecs/keen-eye/issues/81)
- [ADR-004: Reflection Elimination](../adr/004-reflection-elimination.md)

## Changelog

- **2025-12-14**: Initial benchmark project created
  - Runtime performance benchmarks implemented
  - Startup time benchmark application created
  - Binary size measurement documented
  - Awaiting actual benchmark results
