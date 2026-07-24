using System.Collections.Concurrent;
using KeenEyes.Parallelism;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the ParallelSystemPlugin and ParallelSystemScheduler.
/// </summary>
[Collection("ParallelismTests")]
public class ParallelSystemPluginTests
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

    public struct Damage : IComponent
    {
        public int Amount;
    }

    #endregion

    #region Test Systems

    private sealed class MovementSystem : SystemBase, ISystemDependencyProvider
    {
        public int UpdateCount;

        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Velocity>().Writes<Position>();
        }

        public override void Update(float deltaTime)
        {
            Interlocked.Increment(ref UpdateCount);
            foreach (var entity in World.Query<Position, Velocity>())
            {
                ref var pos = ref World.Get<Position>(entity);
                ref readonly var vel = ref World.Get<Velocity>(entity);
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
            }
        }
    }

    private sealed class DamageSystem : SystemBase, ISystemDependencyProvider
    {
        public int UpdateCount;

        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Reads<Damage>().Writes<Health>();
        }

        public override void Update(float deltaTime)
        {
            Interlocked.Increment(ref UpdateCount);
            foreach (var entity in World.Query<Health, Damage>())
            {
                ref var health = ref World.Get<Health>(entity);
                ref readonly var damage = ref World.Get<Damage>(entity);
                health.Current -= damage.Amount;
            }
        }
    }

    private sealed class PhysicsSystem : SystemBase, ISystemDependencyProvider
    {
        public int UpdateCount;

        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<Velocity>(); // Conflicts with MovementSystem
        }

        public override void Update(float deltaTime)
        {
            Interlocked.Increment(ref UpdateCount);
        }
    }

    private sealed class ThreadTrackingSystem : SystemBase
    {
        public ConcurrentBag<int> ThreadIds { get; } = [];

        public override void Update(float deltaTime)
        {
            ThreadIds.Add(Environment.CurrentManagedThreadId);
            // Simulate some work
            Thread.SpinWait(1000);
        }
    }

    /// <summary>
    /// A system that forces a GetHashCode() collision with every other instance while
    /// keeping reference identity for Equals. Used to prove the command-buffer pool is
    /// keyed by a stable per-registration id rather than GetHashCode() (issue #1155).
    /// </summary>
    private sealed class CollidingHashSystem : SystemBase
    {
        public int UpdateCount;

        public override void Update(float deltaTime)
        {
            Interlocked.Increment(ref UpdateCount);
        }

        public override int GetHashCode() => 42;

        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    }

    /// <summary>
    /// Records the maximum number of systems observed executing concurrently, used to
    /// distinguish sequential from parallel batch execution (issue #1159).
    /// </summary>
    private sealed class ConcurrencyProbe
    {
        private int current;
        private int maxObserved;

        public int MaxObserved => Volatile.Read(ref maxObserved);

        public void Observe()
        {
            var now = Interlocked.Increment(ref current);

            int observed;
            while (now > (observed = Volatile.Read(ref maxObserved)))
            {
                Interlocked.CompareExchange(ref maxObserved, now, observed);
            }

            // Hold long enough for a genuinely concurrent sibling to overlap.
            Thread.Sleep(50);
            Interlocked.Decrement(ref current);
        }
    }

    private sealed class ConcurrencyProbeSystem : SystemBase
    {
        public ConcurrencyProbe Probe { get; set; } = new();

        public override void Update(float deltaTime) => Probe.Observe();
    }

    #endregion

    #region Plugin Installation Tests

    [Fact]
    public void Install_CreatesSchedulerExtension()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();

        world.InstallPlugin(plugin);

        var scheduler = world.GetExtension<ParallelSystemScheduler>();
        Assert.NotNull(scheduler);
    }

    [Fact]
    public void Uninstall_RemovesSchedulerExtension()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();

        world.InstallPlugin(plugin);
        world.UninstallPlugin<ParallelSystemPlugin>();

        var hasExtension = world.TryGetExtension<ParallelSystemScheduler>(out _);
        Assert.False(hasExtension);
    }

    [Fact]
    public void Install_WithOptions_UsesConfiguration()
    {
        using var world = new World();
        var options = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 4,
            MinBatchSizeForParallel = 3
        };
        var plugin = new ParallelSystemPlugin(options);

        world.InstallPlugin(plugin);

        var scheduler = world.GetExtension<ParallelSystemScheduler>();
        Assert.NotNull(scheduler);
    }

    #endregion

    #region System Registration Tests

    [Fact]
    public void RegisterSystem_AddsSystemToScheduler()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new MovementSystem();
        system.Initialize(world);
        scheduler.RegisterSystem(system);

        Assert.Equal(1, scheduler.SystemCount);
    }

    [Fact]
    public void RegisterSystem_ExtractsDependencies()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new MovementSystem();
        system.Initialize(world);
        scheduler.RegisterSystem(system);

        var deps = scheduler.DependencyTracker.GetDependencies<MovementSystem>();
        Assert.Contains(typeof(Velocity), deps.Reads);
        Assert.Contains(typeof(Position), deps.Writes);
    }

    [Fact]
    public void RegisterSystem_WithoutDeclaredDependencies_AssumesEmptyAndBatchesConcurrently()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // ThreadTrackingSystem does not implement ISystemDependencyProvider. Per the tracker's
        // documented (optimistic) contract, undeclared dependencies are assumed empty, so two
        // such systems never conflict and are batched together. This pins the behavior the
        // class doc was corrected to describe in issue #1156.
        var a = new ThreadTrackingSystem();
        var b = new ThreadTrackingSystem();
        a.Initialize(world);
        b.Initialize(world);
        scheduler.RegisterSystem(a);
        scheduler.RegisterSystem(b);

        var deps = scheduler.DependencyTracker.GetDependencies<ThreadTrackingSystem>();
        Assert.Empty(deps.Reads);
        Assert.Empty(deps.Writes);

        var batches = scheduler.GetBatches();
        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    [Fact]
    public void UnregisterSystem_RemovesSystem()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new MovementSystem();
        system.Initialize(world);
        scheduler.RegisterSystem(system);
        Assert.Equal(1, scheduler.SystemCount);

        var removed = scheduler.UnregisterSystem(system);

        Assert.True(removed);
        Assert.Equal(0, scheduler.SystemCount);
    }

    [Fact]
    public void UnregisterSystem_WithSurvivingSameTypeInstance_RetainsDependencies()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // Two MovementSystem instances (read Velocity, write Position) plus a PhysicsSystem
        // (writes Velocity). MovementSystem reads what PhysicsSystem writes, so a surviving
        // mover must still conflict with physics after one mover is unregistered.
        var mover1 = new MovementSystem();
        var mover2 = new MovementSystem();
        var physics = new PhysicsSystem();
        mover1.Initialize(world);
        mover2.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(mover1);
        scheduler.RegisterSystem(mover2);
        scheduler.RegisterSystem(physics);

        scheduler.UnregisterSystem(mover1);

        // Regression for #1158: the dependency tracker keys by Type, so unregistering one
        // instance must not drop the type's deps while another instance is still live. If it
        // did, mover2 would fall back to empty deps and merge with physics into one batch.
        var batches = scheduler.GetBatches();
        Assert.Equal(2, batches.Count);
    }

    [Fact]
    public void Clear_RemovesAllSystems()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem();
        var damage = new DamageSystem();
        movement.Initialize(world);
        damage.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(damage);

        scheduler.Clear();

        Assert.Equal(0, scheduler.SystemCount);
    }

    #endregion

    #region Batch Creation Tests

    [Fact]
    public void GetBatches_NoConflicts_SingleBatch()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // Movement (writes Position) and Damage (writes Health) don't conflict
        var movement = new MovementSystem();
        var damage = new DamageSystem();
        movement.Initialize(world);
        damage.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(damage);

        var batches = scheduler.GetBatches();

        Assert.Single(batches);
        Assert.Equal(2, batches[0].Count);
    }

    [Fact]
    public void GetBatches_WithConflicts_MultipleBatches()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // Movement reads Velocity, Physics writes Velocity - conflict
        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        var batches = scheduler.GetBatches();

        Assert.Equal(2, batches.Count);
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public void UpdateParallel_ExecutesAllSystems()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem();
        var damage = new DamageSystem();
        movement.Initialize(world);
        damage.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(damage);

        scheduler.UpdateParallel(0.016f);

        Assert.Equal(1, movement.UpdateCount);
        Assert.Equal(1, damage.UpdateCount);
    }

    [Fact]
    public void UpdateParallel_ModifiesComponents()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // Create entities
        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(new Position { X = 0, Y = 0 })
                .With(new Velocity { X = 100, Y = 50 })
                .Build();
        }

        var movement = new MovementSystem();
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        const float dt = 0.016f;
        scheduler.UpdateParallel(dt);

        // Verify all positions were updated
        foreach (var entity in world.Query<Position>())
        {
            ref var pos = ref world.Get<Position>(entity);
            Assert.Equal(100 * dt, pos.X, 0.001f);
            Assert.Equal(50 * dt, pos.Y, 0.001f);
        }
    }

    [Fact]
    public void UpdateParallel_DisabledSystem_NotExecuted()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem { Enabled = false };
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        scheduler.UpdateParallel(0.016f);

        Assert.Equal(0, movement.UpdateCount);
    }

    [Fact]
    public void UpdateParallel_MultipleUpdates_AccumulatesCorrectly()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem();
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        scheduler.UpdateParallel(0.016f);
        scheduler.UpdateParallel(0.016f);
        scheduler.UpdateParallel(0.016f);

        Assert.Equal(3, movement.UpdateCount);
    }

    [Fact]
    public void UpdateParallel_SystemsWithCollidingHashCodes_ExecuteWithoutBufferCollision()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        // Both systems return GetHashCode() == 42 and have no declared dependencies, so they
        // share one batch. The old scheme keyed the command-buffer pool by GetHashCode(), so
        // the second Rent(42) threw a duplicate-key exception and the batch's commands were
        // discarded. A stable per-registration id keeps their buffers distinct (issue #1155).
        var a = new CollidingHashSystem();
        var b = new CollidingHashSystem();
        a.Initialize(world);
        b.Initialize(world);
        scheduler.RegisterSystem(a);
        scheduler.RegisterSystem(b);

        scheduler.UpdateParallel(0.016f);

        Assert.Equal(1, a.UpdateCount);
        Assert.Equal(1, b.UpdateCount);
    }

    [Fact]
    public void UpdateParallel_BatchBelowMinBatchSize_ExecutesSequentially()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin(new ParallelSystemOptions
        {
            MinBatchSizeForParallel = 3
        }));
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var probe = new ConcurrencyProbe();
        var a = new ConcurrencyProbeSystem { Probe = probe };
        var b = new ConcurrencyProbeSystem { Probe = probe };
        a.Initialize(world);
        b.Initialize(world);
        scheduler.RegisterSystem(a);
        scheduler.RegisterSystem(b);

        // Two non-conflicting systems form a single batch of 2. With the threshold at 3, that
        // batch is below the limit and must run sequentially, so the probe never sees both at
        // once. Before #1159 the option was ignored and the batch always ran in parallel.
        scheduler.UpdateParallel(0.016f);

        Assert.Equal(1, probe.MaxObserved);
    }

    [Fact]
    public void UpdateParallel_BatchAtOrAboveMinBatchSize_ExecutesInParallel()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin(new ParallelSystemOptions
        {
            MinBatchSizeForParallel = 2
        }));
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var probe = new ConcurrencyProbe();
        var a = new ConcurrencyProbeSystem { Probe = probe };
        var b = new ConcurrencyProbeSystem { Probe = probe };
        a.Initialize(world);
        b.Initialize(world);
        scheduler.RegisterSystem(a);
        scheduler.RegisterSystem(b);

        // A batch of 2 at the default threshold of 2 must run in parallel, so both systems
        // overlap. Guards against the threshold fix over-serializing eligible batches (#1159).
        scheduler.UpdateParallel(0.016f);

        Assert.Equal(2, probe.MaxObserved);
    }

    #endregion

    #region Analysis Tests

    [Fact]
    public void GetAnalysis_ReturnsConflictDetails()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem();
        var physics = new PhysicsSystem();
        movement.Initialize(world);
        physics.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(physics);

        var analysis = scheduler.GetAnalysis();

        Assert.Equal(1, analysis.ConflictCount);
        Assert.Equal(2, analysis.BatchCount);
    }

    [Fact]
    public void GetAnalysis_NoConflicts_ReportsMaxParallelism()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var movement = new MovementSystem();
        var damage = new DamageSystem();
        movement.Initialize(world);
        damage.Initialize(world);
        scheduler.RegisterSystem(movement);
        scheduler.RegisterSystem(damage);

        var analysis = scheduler.GetAnalysis();

        Assert.Equal(2, analysis.MaxParallelism);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void UpdateParallel_ProducesDeterministicResults()
    {
        // Run the same update multiple times and verify consistent results
        for (int run = 0; run < 5; run++)
        {
            using var world = new World();
            world.InstallPlugin(new ParallelSystemPlugin());
            var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

            for (int i = 0; i < 10; i++)
            {
                world.Spawn()
                    .With(new Position { X = i * 10f, Y = 0 })
                    .With(new Velocity { X = 1, Y = 2 })
                    .Build();
            }

            var movement = new MovementSystem();
            movement.Initialize(world);
            scheduler.RegisterSystem(movement);

            const float dt = 0.016f;
            scheduler.UpdateParallel(dt);

            // All entities should have same delta applied
            foreach (var entity in world.Query<Position>())
            {
                ref var pos = ref world.Get<Position>(entity);
                Assert.Equal(dt * 2, pos.Y, 0.001f);
            }
        }
    }

    #endregion

    #region MockPluginContext Tests

    [Fact]
    public void Install_WithMockContext_RegistersSchedulerExtension()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context
            .ShouldHaveSetExtension<ParallelSystemScheduler>()
            .ShouldHaveSetExtensionCount(1);
    }

    [Fact]
    public void Install_WithMockContext_CreatesWorkingScheduler()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var scheduler = context.GetSetExtension<ParallelSystemScheduler>();
        Assert.NotNull(scheduler);
        Assert.Equal(0, scheduler.SystemCount);
    }

    [Fact]
    public void Install_WithOptions_CreatesSchedulerWithConfiguration()
    {
        using var world = new World();
        var options = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 4,
            MinBatchSizeForParallel = 3
        };
        var plugin = new ParallelSystemPlugin(options);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var scheduler = context.GetSetExtension<ParallelSystemScheduler>();
        Assert.NotNull(scheduler);
    }

    [Fact]
    public void Install_WithoutWorld_ThrowsInvalidOperationException()
    {
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin); // No world provided

        // MockPluginContext.World throws when no world is provided
        Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
    }

    [Fact]
    public void Install_RegistersNoSystems()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        // ParallelSystemPlugin doesn't register any systems directly
        Assert.Empty(context.RegisteredSystems);
    }

    [Fact]
    public void Install_RegistersNoComponents()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        // ParallelSystemPlugin doesn't register any components
        Assert.Empty(context.RegisteredComponents);
    }

    [Fact]
    public void Install_CreatedScheduler_CanRegisterSystems()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var scheduler = context.GetSetExtension<ParallelSystemScheduler>()!;
        var movement = new MovementSystem();
        movement.Initialize(world);
        scheduler.RegisterSystem(movement);

        Assert.Equal(1, scheduler.SystemCount);
    }

    [Fact]
    public void Install_MultipleInstallations_EachCreatesScheduler()
    {
        using var world1 = new World();
        using var world2 = new World();

        var plugin1 = new ParallelSystemPlugin();
        var plugin2 = new ParallelSystemPlugin();
        var context1 = new MockPluginContext(plugin1, world1);
        var context2 = new MockPluginContext(plugin2, world2);

        plugin1.Install(context1);
        plugin2.Install(context2);

        var scheduler1 = context1.GetSetExtension<ParallelSystemScheduler>();
        var scheduler2 = context2.GetSetExtension<ParallelSystemScheduler>();

        Assert.NotNull(scheduler1);
        Assert.NotNull(scheduler2);
        Assert.NotSame(scheduler1, scheduler2);
    }

    #endregion
}
