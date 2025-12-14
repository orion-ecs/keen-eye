# Startup Time Benchmark

Measures time from process start to first ECS operation (world creation, system registration, entity spawn, first update).

## Running the Benchmark

### Single Run

**JIT Mode:**
```bash
dotnet run -c Release
```

**AOT Mode:**
```bash
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT
./bin/Release/net10.0/linux-x64/publish/StartupTimeBenchmark
```

### Multiple Runs for Statistical Analysis

**JIT Mode (100 runs):**
```bash
for i in {1..100}; do
  dotnet run -c Release 2>&1 | grep "Startup time"
done > jit-results.txt
```

**AOT Mode (100 runs):**
```bash
# First publish
dotnet publish -c Release -r linux-x64 -p:BenchmarkMode=AOT

# Then run 100 times
for i in {1..100}; do
  ./bin/Release/net10.0/linux-x64/publish/StartupTimeBenchmark 2>&1 | grep "Startup time"
done > aot-results.txt
```

## Expected Results

From issue #326:

| Mode | Expected Startup Time |
|------|-----------------------|
| JIT  | ~200ms (cold start)  |
| AOT  | ~50ms (4x faster)    |

## Notes

- JIT startup includes JIT compilation overhead
- AOT startup has no JIT overhead (pre-compiled)
- First run may include OS caching effects
- Run multiple times for statistical significance
