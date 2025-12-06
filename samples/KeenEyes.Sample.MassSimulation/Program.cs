using KeenEyes;
using KeenEyes.Sample.MassSimulation;
using System.Diagnostics;

// =============================================================================
// KEEN EYES ECS - Mass Entity Simulation (100,000+ Entities)
// =============================================================================
// Demonstrates the performance of KeenEyes ECS with large entity counts:
//
// - Archetype-based chunked storage: 128 entities per ~16KB chunk
// - Chunk pooling: Empty chunks are recycled to reduce GC pressure
// - Cache-friendly iteration: Components stored contiguously per chunk
// - Swap-back removal: O(1) entity despawn with chunk compaction
//
// This simulation maintains 100,000 particles with continuous spawn/despawn
// cycles to showcase chunk pool reuse and memory efficiency.
// =============================================================================

Console.WriteLine("KeenEyes ECS - Mass Entity Simulation");
Console.WriteLine(new string('=', 60));
Console.WriteLine();

// Configuration
const int TargetEntityCount = 100_000;
const float WorldWidth = 100f;
const float WorldHeight = 50f;
const float TargetFps = 60f;
const float FixedDeltaTime = 1f / TargetFps;
const int WarmupSeconds = 2;
const int RunSeconds = 10;

Console.WriteLine($"Target entities:     {TargetEntityCount:N0}");
Console.WriteLine($"Chunk size:          {ArchetypeChunk.DefaultCapacity} entities (~16KB)");
Console.WriteLine($"World size:          {WorldWidth} x {WorldHeight}");
Console.WriteLine($"Target FPS:          {TargetFps}");
Console.WriteLine();

// Create world
using var world = new World();

// Configure and register systems
var spawnerSystem = new SpawnerSystem
{
    TargetCount = TargetEntityCount,
    MaxSpawnPerFrame = 10_000,
    WorldWidth = WorldWidth,
    WorldHeight = WorldHeight
};

var movementSystem = new MovementSystem
{
    WorldWidth = WorldWidth,
    WorldHeight = WorldHeight
};

var cleanupSystem = new CleanupSystem();

world
    .AddSystem(spawnerSystem)
    .AddSystem<GravitySystem>()
    .AddSystem(movementSystem)
    .AddSystem<LifetimeSystem>()
    .AddSystem(cleanupSystem);

// Timing and statistics
var stopwatch = Stopwatch.StartNew();
var frameCount = 0;
var totalFrameTime = 0.0;
var maxFrameTime = 0.0;
var minFrameTime = double.MaxValue;

Console.WriteLine("Phase 1: Warming up...");
Console.WriteLine();

// Warmup phase - fill the world with entities
var warmupEnd = TimeSpan.FromSeconds(WarmupSeconds);
while (stopwatch.Elapsed < warmupEnd)
{
    var frameStart = stopwatch.Elapsed.TotalMilliseconds;
    world.Update(FixedDeltaTime);
    var frameTime = stopwatch.Elapsed.TotalMilliseconds - frameStart;

    // Print progress every 0.5 seconds
    if (frameCount % 30 == 0)
    {
        var entityCount = world.Query<Position>().Without<Dead>().Count();
        Console.Write($"\r  Entities: {entityCount,8:N0} / {TargetEntityCount:N0}   ");
    }

    frameCount++;

    // Sleep to approximate target FPS during warmup
    var elapsed = stopwatch.Elapsed.TotalMilliseconds - frameStart;
    var sleepTime = (1000.0 / TargetFps) - elapsed;
    if (sleepTime > 0)
    {
        Thread.Sleep((int)sleepTime);
    }
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine($"Phase 2: Running benchmark for {RunSeconds} seconds...");
Console.WriteLine();

// Reset statistics for benchmark phase
frameCount = 0;
totalFrameTime = 0;
maxFrameTime = 0;
minFrameTime = double.MaxValue;

var totalSpawned = 0;
var totalCleaned = 0;

var benchmarkStart = stopwatch.Elapsed;
var benchmarkEnd = benchmarkStart + TimeSpan.FromSeconds(RunSeconds);
var lastReportTime = benchmarkStart;

while (stopwatch.Elapsed < benchmarkEnd)
{
    var frameStart = stopwatch.Elapsed.TotalMilliseconds;

    world.Update(FixedDeltaTime);

    var frameTime = stopwatch.Elapsed.TotalMilliseconds - frameStart;
    totalFrameTime += frameTime;
    maxFrameTime = Math.Max(maxFrameTime, frameTime);
    minFrameTime = Math.Min(minFrameTime, frameTime);
    frameCount++;

    totalSpawned += spawnerSystem.LastSpawnCount;
    totalCleaned += cleanupSystem.LastCleanupCount;

    // Print progress every second
    if (stopwatch.Elapsed - lastReportTime >= TimeSpan.FromSeconds(1))
    {
        var entityCount = world.Query<Position>().Without<Dead>().Count();
        var avgFrameTime = totalFrameTime / frameCount;
        var actualFps = 1000.0 / avgFrameTime;

        Console.WriteLine($"  Entities: {entityCount,8:N0}  |  FPS: {actualFps,6:F1}  |  " +
                          $"Frame: {avgFrameTime,5:F2}ms (min: {minFrameTime:F2}, max: {maxFrameTime:F2})");

        lastReportTime = stopwatch.Elapsed;
    }
}

// Final statistics
Console.WriteLine();
Console.WriteLine(new string('=', 60));
Console.WriteLine("BENCHMARK RESULTS");
Console.WriteLine(new string('=', 60));
Console.WriteLine();

var finalEntityCount = world.Query<Position>().Without<Dead>().Count();
var avgFrame = totalFrameTime / frameCount;
var actualFps2 = 1000.0 / avgFrame;

Console.WriteLine($"Final entity count:    {finalEntityCount:N0}");
Console.WriteLine($"Total frames:          {frameCount:N0}");
Console.WriteLine($"Average FPS:           {actualFps2:F1}");
Console.WriteLine($"Average frame time:    {avgFrame:F2} ms");
Console.WriteLine($"Min frame time:        {minFrameTime:F2} ms");
Console.WriteLine($"Max frame time:        {maxFrameTime:F2} ms");
Console.WriteLine();

Console.WriteLine("Entity Lifecycle:");
Console.WriteLine($"  Total spawned:       {totalSpawned:N0}");
Console.WriteLine($"  Total cleaned:       {totalCleaned:N0}");
Console.WriteLine($"  Spawn rate:          {totalSpawned / (float)RunSeconds:N0} /sec");
Console.WriteLine($"  Cleanup rate:        {totalCleaned / (float)RunSeconds:N0} /sec");
Console.WriteLine();

// Archetype and chunk statistics
Console.WriteLine("Archetype Statistics:");
Console.WriteLine($"  Archetype count:     {world.ArchetypeManager.Count}");

var totalChunks = 0;
foreach (var archetype in world.ArchetypeManager.AllArchetypes)
{
    totalChunks += archetype.ChunkCount;
}
Console.WriteLine($"  Total chunks:        {totalChunks}");
Console.WriteLine($"  Entities per chunk:  {ArchetypeChunk.DefaultCapacity}");
Console.WriteLine($"  Expected chunks:     {(int)Math.Ceiling(finalEntityCount / (double)ArchetypeChunk.DefaultCapacity)}");
Console.WriteLine();

// Chunk pool statistics
var poolStats = world.ArchetypeManager.ChunkPool.GetStats();
Console.WriteLine("Chunk Pool Statistics:");
Console.WriteLine($"  Total rented:        {poolStats.TotalRented:N0}");
Console.WriteLine($"  Total returned:      {poolStats.TotalReturned:N0}");
Console.WriteLine($"  Total created:       {poolStats.TotalCreated:N0}");
Console.WriteLine($"  Total discarded:     {poolStats.TotalDiscarded:N0}");
Console.WriteLine($"  Currently pooled:    {poolStats.PooledCount}");
Console.WriteLine($"  Pool hit rate:       {poolStats.HitRate:P1}");
Console.WriteLine();

// Memory estimate
var estimatedMemoryMB = (totalChunks * 16.0) / 1024.0; // ~16KB per chunk
Console.WriteLine($"Estimated chunk memory: ~{estimatedMemoryMB:F1} MB");
Console.WriteLine();

Console.WriteLine("Benchmark complete!");
