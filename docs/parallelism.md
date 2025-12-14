# Parallelism Guide

The `KeenEyes.Parallelism` plugin enables parallel execution of systems and provides APIs for manual parallel work distribution. This guide covers parallel system scheduling, the job system, and performance profiling.

## Overview

The parallelism plugin provides:

1. **Parallel System Execution** - Automatically batch systems that don't conflict
2. **Component Dependency Tracking** - Declare read/write dependencies per system
3. **Job System API** - Manual parallel work distribution for advanced use cases
4. **Profiler** - Measure execution times and identify bottlenecks

## Installation

### NuGet Package

```bash
dotnet add package KeenEyes.Parallelism
```

### Plugin Installation

```csharp
using KeenEyes;
using KeenEyes.Parallelism;

using var world = new World();
world.InstallPlugin(new ParallelSystemPlugin());

// Or with options
world.InstallPlugin(new ParallelSystemPlugin(new ParallelSystemOptions
{
    MaxDegreeOfParallelism = 4,
    MinBatchSizeForParallel = 2
}));
```

## Parallel System Scheduling

### How It Works

The scheduler analyzes system component dependencies to determine which systems can run concurrently:

1. **Systems declare dependencies** - What components they read/write
2. **Conflict detection** - Systems that write to the same component conflict
3. **Batch creation** - Non-conflicting systems are grouped into parallel batches
4. **Sequential batch execution** - Batches run one after another; systems within a batch run in parallel

### Declaring Dependencies

Systems implement `ISystemDependencyProvider` to declare their component access patterns:

```csharp
public class MovementSystem : SystemBase, ISystemDependencyProvider
{
    public void GetDependencies(ISystemDependencyBuilder builder)
    {
        builder
            .Reads<Velocity>()    // Read-only access
            .Writes<Position>();  // Read-write access
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}

public class HealthRegenSystem : SystemBase, ISystemDependencyProvider
{
    public void GetDependencies(ISystemDependencyBuilder builder)
    {
        builder.Writes<Health>();
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);
            health.Current = Math.Min(health.Current + 1, health.Max);
        }
    }
}
```

In this example, `MovementSystem` and `HealthRegenSystem` can run in parallel because they access different components.

### Conflict Rules

Systems conflict when:
- **Write-Write** - Both systems write to the same component type
- **Read-Write** - One system reads a component another writes

Systems do NOT conflict when:
- **Read-Read** - Both systems only read the same component (safe for parallel access)

### Registering Systems

```csharp
var scheduler = world.GetExtension<ParallelSystemScheduler>();

// Systems that implement ISystemDependencyProvider
var movement = new MovementSystem();
var healthRegen = new HealthRegenSystem();
movement.Initialize(world);
healthRegen.Initialize(world);

scheduler.RegisterSystem(movement);
scheduler.RegisterSystem(healthRegen);

// Or with explicit dependencies
scheduler.RegisterSystem(customSystem, new ComponentDependencies(
    reads: [typeof(Position)],
    writes: [typeof(Velocity)]));
```

### Running Parallel Updates

```csharp
// In your game loop
while (running)
{
    var deltaTime = CalculateDeltaTime();

    // Execute all systems in parallel batches
    scheduler.UpdateParallel(deltaTime);
}
```

### Analyzing Batches

```csharp
var scheduler = world.GetExtension<ParallelSystemScheduler>();
var analysis = scheduler.GetAnalysis();

Console.WriteLine($"Batches: {analysis.BatchCount}");
Console.WriteLine($"Max Parallelism: {analysis.MaxParallelism}");
Console.WriteLine($"Conflicts: {analysis.ConflictCount}");

// View conflict details
foreach (var conflict in analysis.Conflicts)
{
    Console.WriteLine($"  {conflict.SystemA.Name} <-> {conflict.SystemB.Name}");
    Console.WriteLine($"    Components: {string.Join(", ", conflict.ConflictingComponents.Select(c => c.Name))}");
}
```

## Parallel Query Iteration

For systems processing large numbers of entities, use parallel query extensions:

```csharp
using KeenEyes.Parallelism;

public class ParallelMovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process entities in parallel (chunk-level parallelism)
        World.Query<Position, Velocity>()
            .ForEachParallel((Entity entity, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
            });
    }
}
```

### Configuration

```csharp
// Only parallelize if enough entities (default: 1000)
query.ForEachParallel(action, minEntityCount: 500);

// Read-only variant for better performance when not modifying
query.ForEachParallelReadOnly((Entity e, in Position pos, in Velocity vel) =>
{
    // Read-only access
});
```

### When to Use

- **Large entity counts** - Parallelism overhead is only worth it with many entities
- **Independent processing** - Each entity can be processed without affecting others
- **CPU-bound work** - Heavy calculations benefit most from parallelism

## Job System API

The job system provides fine-grained control over parallel work distribution for advanced scenarios.

### Basic Jobs

```csharp
using KeenEyes.Parallelism;

public struct ProcessDataJob : IJob
{
    public float[] Data { get; init; }
    public float Multiplier { get; init; }

    public void Execute()
    {
        for (int i = 0; i < Data.Length; i++)
        {
            Data[i] *= Multiplier;
        }
    }
}

// Usage
using var scheduler = new JobScheduler();

var job = new ProcessDataJob { Data = myData, Multiplier = 2.0f };
var handle = scheduler.Schedule(job);

// Do other work...

handle.Complete(); // Wait for job to finish
```

### Parallel Jobs

Execute across multiple indices in parallel:

```csharp
public struct UpdatePositionsJob : IParallelJob
{
    public Position[] Positions { get; init; }
    public Velocity[] Velocities { get; init; }
    public float DeltaTime { get; init; }

    public void Execute(int index)
    {
        Positions[index].X += Velocities[index].X * DeltaTime;
        Positions[index].Y += Velocities[index].Y * DeltaTime;
    }
}

// Usage
var job = new UpdatePositionsJob
{
    Positions = positions,
    Velocities = velocities,
    DeltaTime = 0.016f
};

var handle = scheduler.ScheduleParallel(job, positions.Length);
handle.Complete();
```

### Batch Jobs

Process ranges of items for better cache locality:

```csharp
public struct SumBatchJob : IBatchJob
{
    public int[] Values { get; init; }
    public int[] PartialSums { get; init; }

    public void Execute(int startIndex, int count)
    {
        var sum = 0;
        for (int i = startIndex; i < startIndex + count; i++)
        {
            sum += Values[i];
        }
        PartialSums[startIndex / count] = sum;
    }
}

// Usage
var handle = scheduler.ScheduleBatch(job, values.Length, batchSize: 64);
```

### Job Dependencies

Chain jobs together:

```csharp
// First job
var prepareHandle = scheduler.Schedule(new PrepareDataJob { Data = data });

// Second job depends on first
var processHandle = scheduler.Schedule(new ProcessDataJob { Data = data }, prepareHandle);

// Third job depends on second
var finalizeHandle = scheduler.Schedule(new FinalizeJob { Data = data }, processHandle);

// Wait for entire chain
finalizeHandle.Complete();
```

### Combining Dependencies

```csharp
var handle1 = scheduler.Schedule(job1);
var handle2 = scheduler.Schedule(job2);
var handle3 = scheduler.Schedule(job3);

// Wait for all three
var combined = JobHandle.CombineDependencies(handle1, handle2, handle3);
combined.Complete();
```

### Error Handling

```csharp
var handle = scheduler.Schedule(new RiskyJob());
handle.Complete();

if (handle.IsFaulted)
{
    Console.WriteLine($"Job failed: {handle.Exception?.Message}");
}
```

### Scheduler Options

```csharp
var scheduler = new JobScheduler(new JobSchedulerOptions
{
    MaxDegreeOfParallelism = 4  // Limit concurrent threads (-1 for unlimited)
});
```

## Profiler

The profiler helps identify performance bottlenecks in parallel execution.

### Basic Usage

```csharp
var scheduler = world.GetExtension<ParallelSystemScheduler>();
var profiler = new ParallelProfiler(scheduler);

// Start profiling
profiler.Start();

// Run your game loop
for (int i = 0; i < 1000; i++)
{
    profiler.BeginFrame();
    scheduler.UpdateParallel(0.016f);
    profiler.EndFrame();
}

// Stop and analyze
profiler.Stop();
var metrics = profiler.GetMetrics();
```

### Metrics

```csharp
var metrics = profiler.GetMetrics();

Console.WriteLine($"Frames: {metrics.TotalFrames}");
Console.WriteLine($"Avg Frame Time: {metrics.AverageFrameTimeMs:F2}ms");
Console.WriteLine($"Parallel Efficiency: {metrics.ParallelEfficiency:P1}");
Console.WriteLine($"Thread Utilization: {metrics.ThreadUtilization:P1}");
Console.WriteLine($"Unique Threads Used: {metrics.UniqueThreadsUsed}");
Console.WriteLine($"Batches: {metrics.BatchCount}");
Console.WriteLine($"Conflicts: {metrics.ConflictCount}");
Console.WriteLine($"Max Parallelism: {metrics.MaxParallelism}");

// Per-system stats
foreach (var (systemType, stats) in metrics.SystemStats)
{
    Console.WriteLine($"  {systemType.Name}:");
    Console.WriteLine($"    Executions: {stats.ExecutionCount}");
    Console.WriteLine($"    Total: {stats.TotalTimeMs:F2}ms");
    Console.WriteLine($"    Average: {stats.AverageTimeMs:F3}ms");
    Console.WriteLine($"    Min/Max: {stats.MinTimeMs:F3}/{stats.MaxTimeMs:F3}ms");
}

// Bottlenecks
foreach (var bottleneck in metrics.Bottlenecks)
{
    Console.WriteLine($"BOTTLENECK: {bottleneck.SystemType.Name}");
    Console.WriteLine($"  Reason: {bottleneck.Reason}");
}
```

### Timing Report

```csharp
// Get formatted text report
var report = profiler.ExportTimingReport();
Console.WriteLine(report);
File.WriteAllText("profile_report.txt", report);
```

### Dependency Graph Visualization

Export to DOT format for visualization with Graphviz:

```csharp
var dot = profiler.ExportDependencyGraph();
File.WriteAllText("dependencies.dot", dot);

// Then generate image:
// dot -Tpng dependencies.dot -o dependencies.png
```

The graph shows:
- Systems grouped by execution batch (clusters)
- Conflicts as red dashed edges
- Conflicting component names on edges

## Best Practices

### Do

- **Declare accurate dependencies** - Only list components your system actually accesses
- **Prefer reads over writes** - Read-only access allows more parallelism
- **Keep systems focused** - Smaller systems with fewer dependencies parallelize better
- **Profile before optimizing** - Use the profiler to identify actual bottlenecks
- **Use parallel queries for large entity counts** - Set appropriate `minEntityCount` threshold

### Don't

- **Don't share mutable state** - Systems running in parallel must not share data unsafely
- **Don't forget thread safety** - Use `Interlocked` operations or locks when needed
- **Don't over-parallelize** - Parallelism has overhead; use it for substantial work
- **Don't ignore conflicts** - High conflict counts indicate poor parallelism potential

### Thread Safety Considerations

When systems run in parallel:

```csharp
// SAFE: Each entity processed independently
public void Execute(int index)
{
    positions[index].X += velocities[index].X;
}

// SAFE: Atomic operations
public void Execute(int index)
{
    Interlocked.Add(ref totalCount, 1);
}

// UNSAFE: Shared mutable state without synchronization
private int counter;
public void Execute(int index)
{
    counter++; // Race condition!
}
```

## CommandBuffer Integration

Systems in parallel batches receive isolated CommandBuffers:

```csharp
public class SpawnerSystem : SystemBase, ISystemDependencyProvider, ICommandBufferConsumer
{
    private ICommandBuffer? buffer;

    public void GetDependencies(ISystemDependencyBuilder builder)
    {
        builder.Reads<Spawner>();
    }

    public void SetCommandBuffer(ICommandBuffer commandBuffer)
    {
        buffer = commandBuffer;
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Spawner>())
        {
            // Safe to use during parallel execution
            buffer?.Spawn()
                .With(new Position { X = 0, Y = 0 })
                .With(new Velocity { X = 1, Y = 0 });
        }
    }
}
```

CommandBuffers are flushed after each batch completes, ensuring deterministic ordering.

## Next Steps

- [Systems Guide](systems.md) - System design patterns
- [Plugins Guide](plugins.md) - Plugin architecture
- [Command Buffer Guide](command-buffer.md) - Deferred entity operations
