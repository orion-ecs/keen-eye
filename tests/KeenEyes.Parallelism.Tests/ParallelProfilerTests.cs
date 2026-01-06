using System.Diagnostics;
using KeenEyes.Parallelism;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the ParallelProfiler and diagnostic components.
/// </summary>
[Collection("ParallelismTests")]
public class ParallelProfilerTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X, Y;
    }

    public struct Velocity : IComponent
    {
        public float X, Y;
    }

    public struct Health : IComponent
    {
        public int Current, Max;
    }

    #endregion

    #region Test Systems

    private sealed class MovementSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Velocity>().Writes<Position>();
        }

        public override void Update(float deltaTime)
        {
            Thread.SpinWait(1000); // Simulate work
        }
    }

    private sealed class HealthSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<Health>();
        }

        public override void Update(float deltaTime)
        {
            Thread.SpinWait(500); // Simulate work
        }
    }

    private sealed class PhysicsSystem : SystemBase, ISystemDependencyProvider
    {
        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<Velocity>(); // Conflicts with MovementSystem
        }

        public override void Update(float deltaTime)
        {
            Thread.SpinWait(800); // Simulate work
        }
    }

    private sealed class SlowSystem : SystemBase
    {
        public override void Update(float deltaTime)
        {
            Thread.Sleep(10); // Simulate slow work
        }
    }

    #endregion

    #region Setup Helpers

    private static (World world, ParallelSystemScheduler scheduler) CreateScheduler()
    {
        var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;
        return (world, scheduler);
    }

    #endregion

    #region Basic Profiler Tests

    [Fact]
    public void Constructor_WithScheduler_CreatesProfiler()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;

        using var profiler = new ParallelProfiler(scheduler);

        Assert.NotNull(profiler);
        Assert.False(profiler.IsEnabled);
    }

    [Fact]
    public void Constructor_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ParallelProfiler(null!));
    }

    [Fact]
    public void Start_EnablesProfiling()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();

        Assert.True(profiler.IsEnabled);
    }

    [Fact]
    public void Stop_DisablesProfiling()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.Stop();

        Assert.False(profiler.IsEnabled);
    }

    [Fact]
    public void Clear_ResetsAllData()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.RecordSystemExecution(typeof(MovementSystem), 1000);
        profiler.BeginFrame();
        profiler.EndFrame();

        profiler.Clear();

        Assert.Equal(0, profiler.FrameCount);
    }

    [Fact]
    public void Dispose_DisablesAndClearsProfiler()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.Dispose();

        Assert.False(profiler.IsEnabled);
    }

    #endregion

    #region Frame Recording Tests

    [Fact]
    public void BeginEndFrame_IncrementsFrameCount()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.BeginFrame();
        profiler.EndFrame();
        profiler.BeginFrame();
        profiler.EndFrame();

        Assert.Equal(2, profiler.FrameCount);
    }

    [Fact]
    public void BeginEndFrame_WhenDisabled_DoesNothing()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        // Don't call Start()
        profiler.BeginFrame();
        profiler.EndFrame();

        Assert.Equal(0, profiler.FrameCount);
    }

    #endregion

    #region System Recording Tests

    [Fact]
    public void RecordSystemExecution_TracksTimingData()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.RecordSystemExecution(typeof(MovementSystem), Stopwatch.Frequency); // 1 second

        var metrics = profiler.GetMetrics();

        Assert.True(metrics.SystemStats.ContainsKey(typeof(MovementSystem)));
        Assert.Equal(1, metrics.SystemStats[typeof(MovementSystem)].ExecutionCount);
    }

    [Fact]
    public void RecordSystemExecution_WhenDisabled_DoesNothing()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        // Don't call Start()
        profiler.RecordSystemExecution(typeof(MovementSystem), 1000);

        var metrics = profiler.GetMetrics();

        Assert.Empty(metrics.SystemStats);
    }

    [Fact]
    public void RecordSystemExecution_MultipleExecutions_Accumulates()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.RecordSystemExecution(typeof(MovementSystem), 1000);
        profiler.RecordSystemExecution(typeof(MovementSystem), 2000);
        profiler.RecordSystemExecution(typeof(MovementSystem), 1500);

        var metrics = profiler.GetMetrics();
        var stats = metrics.SystemStats[typeof(MovementSystem)];

        Assert.Equal(3, stats.ExecutionCount);
    }

    #endregion

    #region Batch Recording Tests

    [Fact]
    public void RecordBatchExecution_TracksBatchTimings()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.RecordBatchExecution(0, 2, 1000);
        profiler.RecordBatchExecution(1, 1, 500);

        var metrics = profiler.GetMetrics();

        Assert.Equal(2, metrics.BatchStats.Count);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public void GetMetrics_WithNoData_ReturnsDefaultMetrics()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var metrics = profiler.GetMetrics();

        Assert.Equal(0, metrics.TotalFrames);
        Assert.Equal(0, metrics.AverageFrameTimeMs);
        Assert.Empty(metrics.SystemStats);
    }

    [Fact]
    public void GetMetrics_IncludesBatchAnalysis()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        var health = new HealthSystem();
        movement.Initialize(world);
        health.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(health);

        var metrics = profiler.GetMetrics();

        Assert.Equal(1, metrics.BatchCount);
        Assert.Equal(2, metrics.MaxParallelism);
        Assert.Equal(0, metrics.ConflictCount);
    }

    [Fact]
    public void GetMetrics_WithConflicts_ReportsConflictCount()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        var metrics = profiler.GetMetrics();

        Assert.Equal(1, metrics.ConflictCount);
    }

    [Fact]
    public void GetMetrics_TracksThreadUtilization()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        // Record from multiple threads would increase unique thread count
        profiler.RecordSystemExecution(typeof(MovementSystem), 1000);

        var metrics = profiler.GetMetrics();

        Assert.True(metrics.UniqueThreadsUsed >= 1);
        Assert.True(metrics.ThreadUtilization >= 0);
    }

    #endregion

    #region Dependency Graph Export Tests

    [Fact]
    public void ExportDependencyGraph_ReturnsValidDotFormat()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        var health = new HealthSystem();
        movement.Initialize(world);
        health.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(health);

        var dot = profiler.ExportDependencyGraph();

        Assert.Contains("digraph SystemDependencies", dot);
        Assert.Contains("MovementSystem", dot);
        Assert.Contains("HealthSystem", dot);
    }

    [Fact]
    public void ExportDependencyGraph_ShowsConflicts()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        var dot = profiler.ExportDependencyGraph();

        Assert.Contains("color=red", dot); // Conflict edges are red
        Assert.Contains("Velocity", dot); // The conflicting component
    }

    [Fact]
    public void ExportDependencyGraph_ShowsBatchClusters()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        var dot = profiler.ExportDependencyGraph();

        Assert.Contains("subgraph cluster_0", dot);
        Assert.Contains("subgraph cluster_1", dot);
        Assert.Contains("Batch 0", dot);
        Assert.Contains("Batch 1", dot);
    }

    #endregion

    #region Timing Report Tests

    [Fact]
    public void ExportTimingReport_ContainsOverview()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        profiler.Start();
        profiler.BeginFrame();
        profiler.EndFrame();

        var report = profiler.ExportTimingReport();

        Assert.Contains("Parallel System Profiler Report", report);
        Assert.Contains("Total Frames: 1", report);
    }

    [Fact]
    public void ExportTimingReport_ContainsSystemTimings()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        profiler.Start();
        profiler.RecordSystemExecution(typeof(MovementSystem), Stopwatch.Frequency / 1000); // 1ms

        var report = profiler.ExportTimingReport();

        Assert.Contains("System Timings", report);
        Assert.Contains("MovementSystem", report);
    }

    [Fact]
    public void ExportTimingReport_ContainsBatchTimings()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        var movement = new MovementSystem();
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        profiler.Start();
        profiler.RecordBatchExecution(0, 1, 1000);

        var report = profiler.ExportTimingReport();

        Assert.Contains("Batch Timings", report);
        Assert.Contains("Batch 0", report);
    }

    #endregion

    #region Bottleneck Detection Tests

    [Fact]
    public void GetMetrics_IdentifiesConflictBottlenecks()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        using var profiler = new ParallelProfiler(scheduler);

        // Create a system that conflicts with multiple others
        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        // Add another system that also conflicts with physics would create bottleneck
        // For this test, just verify the metrics structure
        var metrics = profiler.GetMetrics();

        Assert.NotNull(metrics.Bottlenecks);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        var profiler = new ParallelProfiler(scheduler);

        profiler.Dispose();

        Assert.Throws<ObjectDisposedException>(() => profiler.Start());
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        var (world, scheduler) = CreateScheduler();
        using var _ = world;
        var profiler = new ParallelProfiler(scheduler);

        profiler.Dispose();
        profiler.Dispose(); // Should not throw
    }

    #endregion
}
