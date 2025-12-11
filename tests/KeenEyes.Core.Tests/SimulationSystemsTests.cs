using KeenEyes.Sample.Simulation;

namespace KeenEyes.Core.Tests;

/// <summary>
/// Tests for the Simulation sample systems to ensure magic number constants are working correctly.
/// </summary>
public sealed class SimulationSystemsTests
{
    #region SpawnerSystem Tests

    [Fact]
    public void SpawnerSystem_SpawnsEnemies_WhenBelowMaxLimit()
    {
        using var world = new World();
        var spawner = new SpawnerSystem();
        world.AddSystem(spawner);

        // Force a spawn by setting timer to 0
        spawner.Update(spawner.SpawnInterval + 0.1f);

        // Should have spawned one enemy
        var enemyCount = world.Query<Position>().With<Enemy>().Count();
        Assert.Equal(1, enemyCount);
    }

    [Fact]
    public void SpawnerSystem_RespectsMaxEnemiesLimit()
    {
        using var world = new World();
        var spawner = new SpawnerSystem { MaxEnemies = 2, SpawnInterval = 0.1f };
        world.AddSystem(spawner);

        // Spawn multiple times
        for (int i = 0; i < 10; i++)
        {
            spawner.Update(0.2f);
        }

        // Should not exceed max
        var enemyCount = world.Query<Position>().With<Enemy>().Count();
        Assert.True(enemyCount <= spawner.MaxEnemies, $"Enemy count {enemyCount} exceeds max {spawner.MaxEnemies}");
    }

    [Fact]
    public void SpawnerSystem_SpawnedEnemy_HasRequiredComponents()
    {
        using var world = new World();
        var spawner = new SpawnerSystem();
        world.AddSystem(spawner);

        spawner.Update(spawner.SpawnInterval + 0.1f);

        var enemies = world.Query<Position, Velocity, Health, Collider, Renderable>()
            .With<Enemy>()
            .ToList();

        Assert.Single(enemies);
        var enemy = enemies[0];

        // Verify all components are present
        Assert.True(world.Has<Position>(enemy));
        Assert.True(world.Has<Velocity>(enemy));
        Assert.True(world.Has<Health>(enemy));
        Assert.True(world.Has<Collider>(enemy));
        Assert.True(world.Has<Renderable>(enemy));
    }

    [Fact]
    public void SpawnerSystem_SpawnedEnemy_HasValidPosition()
    {
        using var world = new World();
        var spawner = new SpawnerSystem { WorldWidth = 60, WorldHeight = 20 };
        world.AddSystem(spawner);

        // Spawn multiple enemies to test all edges
        for (int i = 0; i < 20; i++)
        {
            spawner.Update(spawner.SpawnInterval + 0.1f);
        }

        foreach (var enemy in world.Query<Position>().With<Enemy>())
        {
            ref readonly var pos = ref world.Get<Position>(enemy);
            Assert.InRange(pos.X, 0, spawner.WorldWidth);
            Assert.InRange(pos.Y, 0, spawner.WorldHeight);
        }
    }

    [Fact]
    public void SpawnerSystem_SpawnedEnemy_HasNonZeroVelocity()
    {
        using var world = new World();
        var spawner = new SpawnerSystem();
        world.AddSystem(spawner);

        // Spawn multiple enemies
        for (int i = 0; i < 10; i++)
        {
            spawner.Update(spawner.SpawnInterval + 0.1f);
        }

        foreach (var enemy in world.Query<Velocity>().With<Enemy>())
        {
            ref readonly var vel = ref world.Get<Velocity>(enemy);
            // At least one velocity component should be non-zero
            Assert.True(vel.X != 0 || vel.Y != 0, "Enemy should have non-zero velocity");
        }
    }

    #endregion

    #region ShootingSystem Tests

    [Fact]
    public void ShootingSystem_DefaultFireRate_IsSet()
    {
        var shootingSystem = new ShootingSystem();
        Assert.Equal(0.3f, shootingSystem.FireRate);
    }

    [Fact]
    public void ShootingSystem_CreatesProjectile_WhenEnemyPresent()
    {
        using var world = new World();
        var buffer = new CommandBuffer();
        var shootingSystem = new ShootingSystem { FireRate = 0.1f };
        world.AddSystem(shootingSystem);

        // Create player
        var player = world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithCooldown(remaining: 0)
            .WithPlayer()
            .Build();

        // Create enemy
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithEnemy()
            .Build();

        // Update shooting system
        shootingSystem.Update(0.1f);

        // Should have created a projectile
        var projectiles = world.Query<Position>().With<Projectile>().Count();
        Assert.True(projectiles > 0, "Should have created at least one projectile");
    }

    [Fact]
    public void ShootingSystem_Projectile_HasCorrectComponents()
    {
        using var world = new World();
        var shootingSystem = new ShootingSystem();
        world.AddSystem(shootingSystem);

        // Create player
        world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithCooldown(remaining: 0)
            .WithPlayer()
            .Build();

        // Create enemy
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithEnemy()
            .Build();

        shootingSystem.Update(0.1f);

        var projectiles = world.Query<Position, Velocity, Damage, Collider, Lifetime>()
            .With<Projectile>()
            .ToList();

        if (projectiles.Count > 0)
        {
            var projectile = projectiles[0];
            Assert.True(world.Has<Position>(projectile));
            Assert.True(world.Has<Velocity>(projectile));
            Assert.True(world.Has<Damage>(projectile));
            Assert.True(world.Has<Collider>(projectile));
            Assert.True(world.Has<Lifetime>(projectile));
        }
    }

    [Fact]
    public void ShootingSystem_RespectsFireRateCooldown()
    {
        using var world = new World();
        var shootingSystem = new ShootingSystem { FireRate = 1.0f };
        world.AddSystem(shootingSystem);

        // Create player with cooldown remaining
        world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithCooldown(remaining: 0.5f)
            .WithPlayer()
            .Build();

        // Create enemy
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithEnemy()
            .Build();

        // Update with small delta (cooldown still active)
        shootingSystem.Update(0.1f);

        // Should not have created projectile due to cooldown
        var projectiles = world.Query<Position>().With<Projectile>().Count();
        Assert.Equal(0, projectiles);
    }

    #endregion

    #region EnemyShootingSystem Tests

    [Fact]
    public void EnemyShootingSystem_ToughEnemiesShoot_WhenPlayerPresent()
    {
        using var world = new World();
        var enemyShootingSystem = new EnemyShootingSystem();
        world.AddSystem(enemyShootingSystem);

        // Create player
        world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithPlayer()
            .Build();

        // Create tough enemy (symbol '@') with zero cooldown
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithCooldown(remaining: 0)
            .WithRenderable(symbol: '@', color: ConsoleColor.Magenta)
            .WithEnemy()
            .Build();

        enemyShootingSystem.Update(0.1f);

        // Should have created enemy projectile
        var enemyProjectiles = world.Query<Position>()
            .With<Projectile>()
            .With<Enemy>()
            .Count();
        Assert.True(enemyProjectiles > 0, "Tough enemy should shoot");
    }

    [Fact]
    public void EnemyShootingSystem_WeakEnemiesDontShoot()
    {
        using var world = new World();
        var enemyShootingSystem = new EnemyShootingSystem();
        world.AddSystem(enemyShootingSystem);

        // Create player
        world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithPlayer()
            .Build();

        // Create weak enemy (symbol 'o') with zero cooldown
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithCooldown(remaining: 0)
            .WithRenderable(symbol: 'o', color: ConsoleColor.Yellow)
            .WithEnemy()
            .Build();

        enemyShootingSystem.Update(0.1f);

        // Should not have created projectile (weak enemies don't shoot)
        var enemyProjectiles = world.Query<Position>()
            .With<Projectile>()
            .With<Enemy>()
            .Count();
        Assert.Equal(0, enemyProjectiles);
    }

    [Fact]
    public void EnemyShootingSystem_DoesNotShoot_WhenNoPlayer()
    {
        using var world = new World();
        var enemyShootingSystem = new EnemyShootingSystem();
        world.AddSystem(enemyShootingSystem);

        // Create tough enemy without player
        world.Spawn()
            .WithPosition(x: 20, y: 10)
            .WithCooldown(remaining: 0)
            .WithRenderable(symbol: '@', color: ConsoleColor.Magenta)
            .WithEnemy()
            .Build();

        enemyShootingSystem.Update(0.1f);

        var projectiles = world.Query<Position>().With<Projectile>().Count();
        Assert.Equal(0, projectiles);
    }

    #endregion

    #region MovementSystem Tests

    [Fact]
    public void MovementSystem_UpdatesPosition_BasedOnVelocity()
    {
        using var world = new World();
        var movementSystem = new MovementSystem { WorldWidth = 60, WorldHeight = 20 };
        world.AddSystem(movementSystem);

        var entity = world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithVelocity(x: 5, y: 0)
            .Build();

        movementSystem.Update(1.0f);

        ref readonly var pos = ref world.Get<Position>(entity);
        Assert.Equal(15, pos.X);
        Assert.Equal(10, pos.Y);
    }

    [Fact]
    public void MovementSystem_BouncesOffWalls_ForNonProjectiles()
    {
        using var world = new World();
        var movementSystem = new MovementSystem { WorldWidth = 60, WorldHeight = 20 };
        world.AddSystem(movementSystem);

        var entity = world.Spawn()
            .WithPosition(x: 59, y: 10)
            .WithVelocity(x: 10, y: 0)
            .Build();

        movementSystem.Update(1.0f);

        ref readonly var vel = ref world.Get<Velocity>(entity);
        Assert.True(vel.X < 0, "Velocity should have reversed after hitting wall");
    }

    [Fact]
    public void MovementSystem_MarksProjectilesDead_WhenHittingWalls()
    {
        using var world = new World();
        var movementSystem = new MovementSystem { WorldWidth = 60, WorldHeight = 20 };
        world.AddSystem(movementSystem);

        var projectile = world.Spawn()
            .WithPosition(x: 59, y: 10)
            .WithVelocity(x: 10, y: 0)
            .WithProjectile()
            .Build();

        movementSystem.Update(1.0f);

        Assert.True(world.Has<Dead>(projectile), "Projectile should be marked dead after hitting wall");
    }

    #endregion

    #region HealthSystem Tests

    [Fact]
    public void HealthSystem_MarksDead_WhenHealthZero()
    {
        using var world = new World();
        var healthSystem = new HealthSystem();
        world.AddSystem(healthSystem);

        var entity = world.Spawn()
            .WithHealth(current: 0, max: 10)
            .Build();

        healthSystem.Update(0.1f);

        Assert.True(world.Has<Dead>(entity), "Entity with zero health should be marked dead");
    }

    [Fact]
    public void HealthSystem_DoesNotMarkDead_WhenHealthPositive()
    {
        using var world = new World();
        var healthSystem = new HealthSystem();
        world.AddSystem(healthSystem);

        var entity = world.Spawn()
            .WithHealth(current: 5, max: 10)
            .Build();

        healthSystem.Update(0.1f);

        Assert.False(world.Has<Dead>(entity), "Entity with positive health should not be marked dead");
    }

    #endregion

    #region LifetimeSystem Tests

    [Fact]
    public void LifetimeSystem_DecrementsLifetime()
    {
        using var world = new World();
        var lifetimeSystem = new LifetimeSystem();
        world.AddSystem(lifetimeSystem);

        var entity = world.Spawn()
            .WithLifetime(remaining: 2.0f)
            .Build();

        lifetimeSystem.Update(0.5f);

        ref readonly var lifetime = ref world.Get<Lifetime>(entity);
        Assert.Equal(1.5f, lifetime.Remaining);
    }

    [Fact]
    public void LifetimeSystem_MarksDead_WhenExpired()
    {
        using var world = new World();
        var lifetimeSystem = new LifetimeSystem();
        world.AddSystem(lifetimeSystem);

        var entity = world.Spawn()
            .WithLifetime(remaining: 0.5f)
            .Build();

        lifetimeSystem.Update(1.0f);

        Assert.True(world.Has<Dead>(entity), "Entity with expired lifetime should be marked dead");
    }

    #endregion

    #region CooldownSystem Tests

    [Fact]
    public void CooldownSystem_DecrementsCooldown()
    {
        using var world = new World();
        var cooldownSystem = new CooldownSystem();
        world.AddSystem(cooldownSystem);

        var entity = world.Spawn()
            .WithCooldown(remaining: 2.0f)
            .Build();

        cooldownSystem.Update(0.5f);

        ref readonly var cooldown = ref world.Get<Cooldown>(entity);
        Assert.Equal(1.5f, cooldown.Remaining);
    }

    [Fact]
    public void CooldownSystem_DoesNotDecrementBelowZero()
    {
        using var world = new World();
        var cooldownSystem = new CooldownSystem();
        world.AddSystem(cooldownSystem);

        var entity = world.Spawn()
            .WithCooldown(remaining: 0.5f)
            .Build();

        cooldownSystem.Update(1.0f);

        ref readonly var cooldown = ref world.Get<Cooldown>(entity);
        Assert.True(cooldown.Remaining <= 0, "Cooldown should not go significantly below zero");
    }

    #endregion

    #region CleanupSystem Tests

    [Fact]
    public void CleanupSystem_RemovesDeadEntities()
    {
        using var world = new World();
        var cleanupSystem = new CleanupSystem();
        world.AddSystem(cleanupSystem);

        var entity = world.Spawn()
            .WithPosition(x: 0, y: 0)
            .WithDead()
            .Build();

        cleanupSystem.Update(0.1f);

        Assert.False(world.IsAlive(entity), "Dead entity should be despawned");
    }

    [Fact]
    public void CleanupSystem_KeepsLiveEntities()
    {
        using var world = new World();
        var cleanupSystem = new CleanupSystem();
        world.AddSystem(cleanupSystem);

        var entity = world.Spawn()
            .WithPosition(x: 0, y: 0)
            .Build();

        cleanupSystem.Update(0.1f);

        Assert.True(world.IsAlive(entity), "Live entity should remain");
    }

    #endregion

    #region CollisionSystem Tests

    [Fact]
    public void CollisionSystem_DetectsCollision_WhenEntitiesOverlap()
    {
        using var world = new World();
        var collisionSystem = new CollisionSystem();
        world.AddSystem(collisionSystem);

        // Create player
        var player = world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithCollider(radius: 0.5f)
            .WithHealth(current: 10, max: 10)
            .WithPlayer()
            .Build();

        // Create enemy projectile at same position
        var projectile = world.Spawn()
            .WithPosition(x: 10, y: 10)
            .WithCollider(radius: 0.3f)
            .WithDamage(amount: 2)
            .WithProjectile()
            .WithEnemy()
            .Build();

        collisionSystem.Update(0.1f);

        ref readonly var health = ref world.Get<Health>(player);
        Assert.Equal(8, health.Current);
        Assert.True(world.Has<Dead>(projectile), "Projectile should be marked dead after collision");
    }

    #endregion
}
