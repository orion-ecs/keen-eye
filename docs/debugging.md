# Debugging & Profiling

The `KeenEyes.Debugging` library provides system profiling, GC allocation tracking, query
diagnostics, entity inspection, log capture, and execution timeline recording for a KeenEyes
`World`. It ships as a single plugin, `DebugPlugin`, that wires these tools together using
`SystemHooks` and the world's capability interfaces.

> **Note:** An earlier example in [Plugins Guide](plugins.md) shows a hand-rolled
> `DebugPlugin` used purely to illustrate the `IWorldPlugin` interface. That example is not the
> library described here — this page documents the real `KeenEyes.Debugging.DebugPlugin`.

## What is KeenEyes.Debugging?

`KeenEyes.Debugging` bundles several independent diagnostic extensions:

1. **`DebugController`** - a central toggle for "debug mode" that other systems can query.
2. **`Profiler`** - per-system execution timing (total, average, min, max).
3. **`GCTracker`** - per-system GC allocation tracking.
4. **`QueryProfiler`** - manual query timing plus automatic query-cache statistics.
5. **`MemoryTracker`** - world and archetype memory statistics and formatted reports.
6. **`EntityInspector`** - runtime inspection of an entity's components, name, and hierarchy.
7. **`LogCapture`** - buffers log entries from an `ILogQueryable` provider during a debug session.
8. **`TimelineRecorder`** / **`TimelineExporter`** (in `KeenEyes.Debugging.Timeline`) - frame-by-frame
   execution history with JSON/CSV export.

Each of these is installed as a `World` extension and retrieved with `IWorld.GetExtension<T>`.
`DebugPlugin` only installs the extensions whose prerequisites are met and whose feature flag
is enabled, so unused tools have zero footprint.

## Quick Start

### Installation

```csharp
using KeenEyes.Debugging;

using var world = new World();

// Install with default options (profiling, GC tracking, and query profiling enabled)
world.InstallPlugin(new DebugPlugin());
```

`DebugPlugin`'s constructor takes an optional `DebugOptions` record:

```csharp
using KeenEyes.Debugging;

var options = new DebugOptions
{
    InitialDebugMode = false,
    EnableProfiling = true,
    EnableGCTracking = true,
    EnableQueryProfiling = true,
    EnableTimeline = false,
    TimelineMaxFrames = 300,
    EnableLogCapture = false
};

using var world = new World();
world.InstallPlugin(new DebugPlugin(options));
```

During `Install`, `DebugPlugin`:

- Always installs `DebugController`, seeded with `DebugOptions.InitialDebugMode`.
- Installs `EntityInspector` only if the world exposes `IInspectionCapability` (it also picks up
  `IHierarchyCapability` when available, for parent/child lookups).
- Installs `MemoryTracker` (and `QueryProfiler`, if `EnableQueryProfiling` is true) only if the
  world exposes `IStatisticsCapability`.
- Installs `Profiler`, `GCTracker`, and `TimelineRecorder` via `SystemHooks`, obtained from
  `ISystemHookCapability`. If any of `EnableProfiling`, `EnableGCTracking`, or `EnableTimeline` is
  true and the world does not support system hooks, `Install` throws `InvalidOperationException`.
- Installs `LogCapture` only if `EnableLogCapture` is true, using the `ILogQueryable` passed as
  `DebugOptions.LogQueryable`.

Retrieve the tools you need through the world's extension API:

```csharp
var profiler = world.GetExtension<Profiler>();
var gcTracker = world.GetExtension<GCTracker>();
var memoryTracker = world.GetExtension<MemoryTracker>();
var queryProfiler = world.GetExtension<QueryProfiler>();
var inspector = world.GetExtension<EntityInspector>();
var controller = world.GetExtension<DebugController>();
```

## Core Concepts

### Debug Mode

`DebugController` is a lightweight, always-installed switch that other code can query before
doing expensive diagnostic work. It raises `DebugModeChanged` whenever `IsDebugMode` changes,
tracks a `ToggleCount` and `LastToggleTime`, and offers a couple of convenience helpers:

```csharp
var controller = world.GetExtension<DebugController>();

controller.DebugModeChanged += (_, enabled) =>
{
    Console.WriteLine(enabled ? "Debug mode ON" : "Debug mode OFF");
};

controller.Enable();
controller.Toggle();

// Only run expensive validation when debug mode is active
controller.WhenDebug(() => ValidateAllEntityReferences());

// Pick a value based on debug mode without an if/else
var resolution = controller.Select(
    debugValue: TimeSpan.FromMicroseconds(1),
    releaseValue: TimeSpan.FromMilliseconds(1));
```

`DebugOptions.OnDebugModeChanged` lets you wire the toggle straight into another system (for
example, adjusting a `LogManager.MinimumLevel`) without subscribing to the event yourself.

### System Profiling

`Profiler` is driven by a pair of `SystemHooks` that `DebugPlugin` registers automatically when
`EnableProfiling` is true. Each system's execution is bracketed with `BeginSample`/`EndSample`,
keyed by the system's type name:

```csharp
var profiler = world.GetExtension<Profiler>();

world.Update(0.016f);

foreach (var profile in profiler.GetAllSystemProfiles())
{
    Console.WriteLine(
        $"{profile.Name}: avg {profile.AverageTime.TotalMilliseconds:F2}ms " +
        $"(min {profile.MinTime.TotalMilliseconds:F2}ms, max {profile.MaxTime.TotalMilliseconds:F2}ms, " +
        $"calls {profile.CallCount})");
}

profiler.Reset();
```

`SystemProfile` (a `readonly record struct`) exposes `Name`, `TotalTime`, `CallCount`,
`AverageTime`, `MinTime`, and `MaxTime`. `DebugOptions.ProfilingPhase` restricts profiling to a
single `SystemPhase` when set; leaving it `null` profiles every phase.

### GC Allocation Tracking

`GCTracker` uses `GC.GetAllocatedBytesForCurrentThread()` around each system's execution to spot
allocation hotspots, following the same `BeginTracking`/`EndTracking` pairing driven by
`SystemHooks`:

```csharp
var gcTracker = world.GetExtension<GCTracker>();

foreach (var profile in gcTracker.GetAllAllocationProfiles())
{
    if (profile.TotalBytes > 1024)
    {
        Console.WriteLine($"{profile.Name} allocated {profile.TotalBytes} bytes over {profile.CallCount} calls");
    }
}

// Formatted report of every tracked system, sorted by total bytes
Console.WriteLine(gcTracker.GetAllocationReport());
```

Because it only measures allocations on the thread where systems execute, allocations made on
worker threads in multi-threaded scenarios are not captured. `DebugOptions.GCTrackingPhase` filters
tracking to a single phase, just like profiling.

### Query Profiling

Unlike system execution, queries run inline inside system code with no automatic hook point, so
`QueryProfiler` requires manual instrumentation with `BeginQuery`/`EndQuery` around the query you
want to measure:

```csharp
var queryProfiler = world.GetExtension<QueryProfiler>();

queryProfiler.BeginQuery("MovementQuery");
var count = 0;
foreach (var entity in world.Query<Position, Velocity>())
{
    count++;
    // process entity
}
queryProfiler.EndQuery("MovementQuery", entityCount: count);

var profile = queryProfiler.GetQueryProfile("MovementQuery");
Console.WriteLine($"Avg: {profile.AverageTime.TotalMicroseconds:F2}us over {profile.AverageEntities} entities");
```

`QueryProfiler` also surfaces cache statistics automatically, with no instrumentation required,
via `IStatisticsCapability`:

```csharp
var cacheStats = queryProfiler.GetCacheStatistics();
Console.WriteLine($"Cache hit rate: {cacheStats.HitRate:F1}% ({cacheStats.CachedQueryCount} cached queries)");

// Slowest 10 queries by average time, plus cache stats, as a formatted report
Console.WriteLine(queryProfiler.GetQueryReport());
```

### Memory & Archetype Statistics

`MemoryTracker` wraps `IStatisticsCapability` with formatted reporting:

```csharp
var memoryTracker = world.GetExtension<MemoryTracker>();

var stats = memoryTracker.GetMemoryStats();
Console.WriteLine($"Active entities: {stats.EntitiesActive}, archetypes: {stats.ArchetypeCount}");

Console.WriteLine(memoryTracker.GetMemoryReport());
Console.WriteLine(memoryTracker.GetArchetypeReport());
```

`GetArchetypeStats()` returns `ArchetypeStatistics` for every archetype currently in use,
including `EntityCount`, `ChunkCount`, `EstimatedMemoryBytes`, and `ComponentTypeNames`, which
`GetArchetypeReport()` renders as a table sorted by entity count.

### Entity Inspection

`EntityInspector` is only installed when the world supports `IInspectionCapability`. Call
`Inspect` to snapshot an entity's name, components, and (if `IHierarchyCapability` is also
available) parent/children:

```csharp
var inspector = world.GetExtension<EntityInspector>();
var info = inspector.Inspect(entity);

Console.WriteLine($"Entity {info.Entity.Id} ({info.Name ?? "unnamed"})");
foreach (var component in info.Components)
{
    Console.WriteLine($"  - {component.TypeName} ({component.SizeInBytes} bytes)");
}
```

`Inspect` throws `InvalidOperationException` if the entity is dead or invalid. `EntityInspector`
also exposes `GetAllEntities()` and `HasComponent`/`HasComponent<T>` for lighter-weight checks
without a full inspection.

### Timeline Recording

`TimelineRecorder` (in `KeenEyes.Debugging.Timeline`) captures per-system, per-frame execution
history when `DebugOptions.EnableTimeline` is true. It is driven by the same `SystemHooks`
mechanism as `Profiler` and `GCTracker`, and keeps only a rolling window of frames
(`DebugOptions.TimelineMaxFrames`, default 300):

```csharp
var recorder = world.GetExtension<TimelineRecorder>();

// Recent history and per-frame slices
var recent = recorder.GetRecentEntries(100);
var frame = recorder.GetEntriesForFrame(recorder.CurrentFrame);

// Aggregate stats per system
var stats = recorder.GetSystemStats();
```

Each `TimelineEntry` records `FrameNumber`, `SystemName`, `Phase`, `StartTicks`, `Duration`, and
`DeltaTime`. `TimelineExporter` turns recorded entries or `TimelineSystemStats` into JSON or CSV
for external visualization tools, or a quick human-readable report:

```csharp
using KeenEyes.Debugging.Timeline;

var json = TimelineExporter.ToJson(recorder.GetAllEntries());
var csv = TimelineExporter.ToCsv(recorder.GetAllEntries());

Console.WriteLine(TimelineExporter.GenerateReport(recorder));
```

Note that `TimelineRecorder` does not advance its own frame counter automatically - call
`AdvanceFrame()` once per frame (for example, right after `world.Update(deltaTime)`) so entries
are correctly bucketed and old frames are trimmed.

### Log Capture

`LogCapture` bridges `KeenEyes.Logging` into a debug session by subscribing to an
`ILogQueryable` provider's `LogAdded` event while capturing is active:

```csharp
using KeenEyes.Debugging;

var options = new DebugOptions
{
    EnableLogCapture = true,
    LogQueryable = ringBufferLogProvider, // an ILogQueryable, e.g. a ring-buffer provider
    LogCaptureMaxEntries = 5_000,
    AutoStartLogCaptureOnDebugMode = true
};

world.InstallPlugin(new DebugPlugin(options));

var logCapture = world.GetExtension<LogCapture>();
logCapture.StartCapture();

world.Update(0.016f);

logCapture.StopCapture();
foreach (var entry in logCapture.GetCapturedEntries(LogLevel.Warning))
{
    Console.WriteLine($"[{entry.Timestamp}] {entry.Level}: {entry.Message}");
}
```

When `AutoStartLogCaptureOnDebugMode` is true (the default), `DebugPlugin` starts and stops
`LogCapture` automatically whenever `DebugController.IsDebugMode` changes, so you don't need to
call `StartCapture`/`StopCapture` yourself in that mode. `GetCapturedEntries` also accepts a
category wildcard pattern (`*`/`?`) for filtering by category instead of level.

## Performance

- Disabled features carry no runtime cost: `DebugPlugin` only registers the `SystemHooks` or
  extensions for features whose `DebugOptions` flag is enabled.
- `Profiler` and `GCTracker` add one hook invocation before and after every system; keep this in
  mind if you also register your own `SystemHooks` for other cross-cutting concerns, since hook
  overhead scales with the total number of registered hooks.
- `QueryProfiler`'s manual timing (`BeginQuery`/`EndQuery`) only costs what you instrument -
  cache statistics from `IStatisticsCapability` are effectively free since the ECS already tracks
  them.
- `TimelineRecorder` trims frames older than `TimelineMaxFrames` on every `AdvanceFrame()` call,
  bounding memory growth; raise the limit for longer replay windows at the cost of more retained
  entries.
- `LogCapture` bounds its buffer at `LogCaptureMaxEntries`, discarding the oldest entry once the
  limit is reached.

## Next Steps

- [Plugins Guide](plugins.md) - How the plugin/capability system that `DebugPlugin` builds on works
- [System Hooks](systems.md) - The `SystemHooks` mechanism used for profiling, GC tracking, and timeline recording
- [Logging](logging.md) - `KeenEyes.Logging` providers and `ILogQueryable` for use with `LogCapture`
- [TestBridge Architecture Guide](testbridge.md) - External tooling that can query similar world state over MCP
