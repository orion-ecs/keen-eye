using System.Collections.Concurrent;
using KeenEyes.Parallelism;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the ParallelSystemPlugin and ParallelSystemScheduler.
/// </summary>
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
                    .With(new Position { X = i * 10, Y = 0 })
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
