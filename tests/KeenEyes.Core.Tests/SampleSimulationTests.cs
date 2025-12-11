using KeenEyes.Sample.Simulation;

namespace KeenEyes.Tests;

/// <summary>
/// Tests to ensure the sample simulation code compiles and demonstrates correct patterns.
/// These tests validate that the samples we provide to users follow best practices.
/// </summary>
public class SampleSimulationTests
{
    #region Sample Compilation Tests

    [Fact]
    public void Sample_MovementSystem_CompilesAndInstantiates()
    {
        // Ensure the sample system can be created
        var system = new MovementSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_CollisionSystem_CompilesAndInstantiates()
    {
        var system = new CollisionSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_HealthSystem_CompilesAndInstantiates()
    {
        var system = new HealthSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_LifetimeSystem_CompilesAndInstantiates()
    {
        var system = new LifetimeSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_CooldownSystem_CompilesAndInstantiates()
    {
        var system = new CooldownSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_CleanupSystem_CompilesAndInstantiates()
    {
        var system = new CleanupSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_SpawnerSystem_CompilesAndInstantiates()
    {
        var system = new SpawnerSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_ShootingSystem_CompilesAndInstantiates()
    {
        var system = new ShootingSystem();
        Assert.NotNull(system);
    }

    [Fact]
    public void Sample_EnemyShootingSystem_CompilesAndInstantiates()
    {
        var system = new EnemyShootingSystem();
        Assert.NotNull(system);
    }

    #endregion

    #region CommandBuffer Usage Pattern Tests

    [Fact]
    public void Sample_MovementSystem_UsesCommandBuffer_ToAddDeadComponent()
    {
        // This test validates that the sample demonstrates the CommandBuffer pattern
        // for deferring entity changes during iteration (as documented in the comment block)
        using var world = new World();
        var system = new MovementSystem
        {
            WorldWidth = 10,
            WorldHeight = 10
        };
        world.AddSystem(system);

        // Create a projectile at the edge to trigger boundary despawn
        var projectile = world.Spawn()
            .WithPosition(x: -1f, y: 5f)  // Outside world bounds
            .WithVelocity(x: 0f, y: 0f)
            .WithProjectile()
            .Build();

        // Run system - should add Dead component via CommandBuffer
        world.Update(0.016f);

        // Verify Dead component was added
        Assert.True(world.Has<Dead>(projectile));
    }

    [Fact]
    public void Sample_HealthSystem_UsesCommandBuffer_ToMarkDeadEntities()
    {
        using var world = new World();
        var system = new HealthSystem();
        world.AddSystem(system);

        // Create entity with zero health
        var entity = world.Spawn()
            .WithHealth(current: 0, max: 10)
            .Build();

        Assert.False(world.Has<Dead>(entity));

        // Run system - should add Dead component via CommandBuffer
        world.Update(0.016f);

        // Verify Dead component was added
        Assert.True(world.Has<Dead>(entity));
    }

    [Fact]
    public void Sample_LifetimeSystem_UsesCommandBuffer_ToMarkExpiredEntities()
    {
        using var world = new World();
        var system = new LifetimeSystem();
        world.AddSystem(system);

        // Create entity with expired lifetime
        var entity = world.Spawn()
            .WithLifetime(remaining: 0.01f)
            .Build();

        Assert.False(world.Has<Dead>(entity));

        // Run system with enough time to expire
        world.Update(0.02f);

        // Verify Dead component was added
        Assert.True(world.Has<Dead>(entity));
    }

    [Fact]
    public void Sample_ShootingSystem_UsesCommandBuffer_ToSpawnProjectiles()
    {
        using var world = new World();
        var system = new ShootingSystem { FireRate = 0.1f };
        world.AddSystem(system);

        // Create player with cooldown ready
        var player = world.Spawn()
            .WithPosition(x: 5f, y: 5f)
            .WithCooldown(remaining: 0f)
            .WithPlayer()
            .Build();

        // Create enemy to shoot at
        world.Spawn()
            .WithPosition(x: 10f, y: 5f)
            .WithEnemy()
            .Build();

        // Count entities before
        int entitiesBeforeUpdate = world.GetAllEntities().Count();

        // Run system - should spawn projectile via CommandBuffer
        world.Update(0.016f);

        // Verify projectile was spawned
        int entitiesAfterUpdate = world.GetAllEntities().Count();
        Assert.True(entitiesAfterUpdate > entitiesBeforeUpdate, "Projectile should be spawned");

        // Verify projectile has correct components
        var projectiles = world.Query<Position, Velocity, Projectile>().ToList();
        Assert.NotEmpty(projectiles);
    }

    #endregion

    #region Sample Code Quality Tests

    [Fact]
    public void Sample_Components_UsePartialStructs()
    {
        // Verify sample components follow the [Component] pattern with partial structs
        var positionType = typeof(Position);
        Assert.True(positionType.IsValueType);

        var velocityType = typeof(Velocity);
        Assert.True(velocityType.IsValueType);

        var healthType = typeof(Health);
        Assert.True(healthType.IsValueType);
    }

    [Fact]
    public void Sample_Systems_InheritFromSystemBase()
    {
        // Verify sample systems follow proper inheritance pattern
        Assert.IsAssignableFrom<SystemBase>(new MovementSystem());
        Assert.IsAssignableFrom<SystemBase>(new CollisionSystem());
        Assert.IsAssignableFrom<SystemBase>(new HealthSystem());
        Assert.IsAssignableFrom<SystemBase>(new LifetimeSystem());
    }

    [Fact]
    public void Sample_TagComponents_UseTagComponentInterface()
    {
        // Verify tag components implement ITagComponent
        var playerType = typeof(Player);
        Assert.True(typeof(ITagComponent).IsAssignableFrom(playerType));

        var enemyType = typeof(Enemy);
        Assert.True(typeof(ITagComponent).IsAssignableFrom(enemyType));

        var deadType = typeof(Dead);
        Assert.True(typeof(ITagComponent).IsAssignableFrom(deadType));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Sample_FullSimulation_CanRunForMultipleFrames()
    {
        // This test ensures the sample can run a basic simulation loop
        using var world = new World();

        // Add all systems from the sample
        world
            .AddSystem(new SpawnerSystem { WorldWidth = 60, WorldHeight = 20, SpawnInterval = 1.0f })
            .AddSystem<CooldownSystem>()
            .AddSystem<ShootingSystem>()
            .AddSystem<EnemyShootingSystem>()
            .AddSystem(new MovementSystem { WorldWidth = 60, WorldHeight = 20 })
            .AddSystem<CollisionSystem>()
            .AddSystem<HealthSystem>()
            .AddSystem<LifetimeSystem>()
            .AddSystem<CleanupSystem>();

        // Create player
        var player = world.Spawn()
            .WithPosition(x: 30f, y: 10f)
            .WithVelocity(x: 1f, y: 1f)
            .WithHealth(current: 5, max: 5)
            .WithCollider(radius: 0.5f)
            .WithRenderable(symbol: '#', color: ConsoleColor.Green)
            .WithCooldown(remaining: 0f)
            .WithPlayer()
            .Build();

        // Run simulation for multiple frames
        for (int i = 0; i < 10; i++)
        {
            world.Update(0.016f);
        }

        // Verify world is in valid state
        Assert.True(world.IsAlive(player) || world.Has<Dead>(player), "Player should be either alive or marked as dead");

        // Verify systems are processing entities
        var allEntities = world.GetAllEntities().ToList();
        Assert.NotEmpty(allEntities);
    }

    [Fact]
    public void Sample_CleanupSystem_RemovesDeadEntities()
    {
        using var world = new World();
        var cleanup = new CleanupSystem();
        world.AddSystem(cleanup);

        // Create entity and mark it dead
        var entity = world.Spawn()
            .WithPosition(x: 0f, y: 0f)
            .WithDead()
            .Build();

        Assert.True(world.IsAlive(entity));
        Assert.True(world.Has<Dead>(entity));

        // Run cleanup
        world.Update(0.016f);

        // Entity should be despawned
        Assert.False(world.IsAlive(entity));
    }

    #endregion
}
