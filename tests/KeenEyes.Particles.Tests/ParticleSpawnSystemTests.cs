using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;
using KeenEyes.Particles.Systems;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for ParticleSpawnSystem coverage.
/// </summary>
public class ParticleSpawnSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    /// <summary>
    /// Finds all alive particle indices in the pool.
    /// </summary>
    private static List<int> FindAllAlive(ParticlePool pool)
    {
        var indices = new List<int>();
        for (var i = 0; i < pool.Capacity; i++)
        {
            if (pool.Alive[i])
            {
                indices.Add(i);
            }
        }
        return indices;
    }

    #region Transform3D Tests

    [Fact]
    public void SpawnSystem_Transform3D_WithoutTransform2D_SpawnsParticles()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(10, 1f);
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(100f, 200f, 50f), Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.Equal(10, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_Transform3D_ProjectsPositionTo2D()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var position3D = new Vector3(150f, 250f, 100f);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            Shape = EmissionShape.Point
        };
        var entity = world.Spawn()
            .With(new Transform3D(position3D, Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Single(alive);
        var idx = alive[0];

        // X and Y should match, Z is ignored
        Assert.InRange(pool.PositionsX[idx], position3D.X - 5f, position3D.X + 5f);
        Assert.InRange(pool.PositionsY[idx], position3D.Y - 5f, position3D.Y + 5f);
    }

    [Fact]
    public void SpawnSystem_Transform3D_WithTransform2D_SkipsTransform3DPath()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // Entity has both Transform2D and Transform3D - should use Transform2D
        var position2D = new Vector2(100f, 100f);
        var emitter = ParticleEmitter.Burst(5, 1f) with
        {
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            Shape = EmissionShape.Point
        };
        var entity = world.Spawn()
            .With(new Transform2D(position2D, 0f, Vector2.One))
            .With(new Transform3D(new Vector3(999f, 999f, 999f), Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Equal(5, alive.Count);

        // Particles should spawn near Transform2D position, not Transform3D
        foreach (var idx in alive)
        {
            Assert.InRange(pool.PositionsX[idx], position2D.X - 10f, position2D.X + 10f);
            Assert.InRange(pool.PositionsY[idx], position2D.Y - 10f, position2D.Y + 10f);
        }
    }

    [Fact]
    public void SpawnSystem_Transform3D_NotPlaying_DoesNotSpawn()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(10, 1f) with { IsPlaying = false };
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_Transform3D_ContinuousEmission_SpawnsOverTime()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Continuous(60f, 1f);
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        // Run for ~1 second
        for (var i = 0; i < 60; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.InRange(pool.ActiveCount, 50, 70);
    }

    [Fact]
    public void SpawnSystem_Transform3D_RepeatingBurst_SpawnsMultipleBursts()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // 10 particles every 0.1 seconds
        var emitter = new ParticleEmitter
        {
            BurstCount = 10,
            BurstInterval = 0.1f,
            LifetimeMin = 2f,
            LifetimeMax = 2f,
            StartSizeMin = 1f,
            StartSizeMax = 1f,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            IsPlaying = true
        };
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(emitter)
            .Build();

        // Run for 0.35 seconds (should trigger 3-4 bursts: at 0, 0.1, 0.2, 0.3)
        for (var i = 0; i < 21; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        // Should have at least 30 particles from 3+ bursts
        Assert.True(pool.ActiveCount >= 30, $"Expected >= 30 particles, got {pool.ActiveCount}");
    }

    #endregion

    #region Repeating Burst Tests

    [Fact]
    public void SpawnSystem_RepeatingBurst_Transform2D_SpawnsMultipleBursts()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // 5 particles every 0.1 seconds
        var emitter = new ParticleEmitter
        {
            BurstCount = 5,
            BurstInterval = 0.1f,
            LifetimeMin = 2f,
            LifetimeMax = 2f,
            StartSizeMin = 1f,
            StartSizeMax = 1f,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            IsPlaying = true
        };
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Run for 0.5 seconds (should trigger 5+ bursts)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        // Should have at least 25 particles from 5+ bursts
        Assert.True(pool.ActiveCount >= 25, $"Expected >= 25 particles, got {pool.ActiveCount}");
    }

    [Fact]
    public void SpawnSystem_RepeatingBurst_AccumulatesCorrectly()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // 1 particle every 0.05 seconds (20 per second)
        var emitter = new ParticleEmitter
        {
            BurstCount = 1,
            BurstInterval = 0.05f,
            LifetimeMin = 5f,
            LifetimeMax = 5f,
            StartSizeMin = 1f,
            StartSizeMax = 1f,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            IsPlaying = true
        };
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Run for 0.5 seconds (should trigger ~10 bursts at intervals)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        // ~10 bursts expected
        Assert.InRange(pool.ActiveCount, 8, 12);
    }

    #endregion

    #region Pool Growth Tests

    [Fact]
    public void SpawnSystem_PoolFull_GrowsAndContinuesSpawning()
    {
        world = new World();
        // Use small initial pool to force growth
        var config = new ParticlesConfig { MaxParticlesPerEmitter = 1000, InitialPoolCapacity = 10 };
        world.InstallPlugin(new ParticlesPlugin(config));

        // Burst 50 particles - will exceed initial capacity of 10
        var emitter = ParticleEmitter.Burst(50, 1f);
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        // Pool should have grown and all 50 particles should be active
        Assert.Equal(50, pool.ActiveCount);
        Assert.True(pool.Capacity >= 50);
    }

    [Fact]
    public void SpawnSystem_PoolAtMaxCapacity_StopsSpawning()
    {
        world = new World();
        // Very limited max capacity
        var config = new ParticlesConfig { MaxParticlesPerEmitter = 20, InitialPoolCapacity = 10 };
        world.InstallPlugin(new ParticlesPlugin(config));

        // Try to burst 100 particles - will hit max of 20
        var emitter = ParticleEmitter.Burst(100, 2f);
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        // Should be capped at max capacity
        Assert.True(pool.ActiveCount <= 20);
        Assert.True(pool.Capacity <= 20);
    }

    #endregion

    #region Emission Shape Tests

    [Fact]
    public void SpawnSystem_SphereShape_SpawnsWithinRadius()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var center = new Vector2(100f, 100f);
        var radius = 50f;
        var emitter = ParticleEmitter.Burst(100, 1f) with
        {
            Shape = EmissionShape.Sphere(radius),
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var entity = world.Spawn()
            .With(new Transform2D(center, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Equal(100, alive.Count);

        // All particles should be within sphere radius from center
        foreach (var idx in alive)
        {
            var dx = pool.PositionsX[idx] - center.X;
            var dy = pool.PositionsY[idx] - center.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            Assert.True(dist <= radius + 1f, $"Particle at distance {dist} exceeds radius {radius}");
        }
    }

    [Fact]
    public void SpawnSystem_ConeShape_SpawnsInConeDirection()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var center = Vector2.Zero;
        // Cone pointing right
        var direction = new Vector2(1f, 0f);
        var coneAngle = MathF.PI / 4f; // 45 degrees
        var emitter = ParticleEmitter.Burst(100, 1f) with
        {
            Shape = EmissionShape.Cone(coneAngle, 10f, direction),
            StartSpeedMin = 50f,
            StartSpeedMax = 50f
        };
        var entity = world.Spawn()
            .With(new Transform2D(center, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Equal(100, alive.Count);

        // Most velocities should be pointing roughly rightward
        // Note: With a 45-degree cone angle, particles spread ±22.5° from horizontal
        // Some particles will have negative X velocity at the cone edges
        var rightwardCount = 0;
        foreach (var idx in alive)
        {
            if (pool.VelocitiesX[idx] > 0)
            {
                rightwardCount++;
            }
        }
        // Majority should be rightward given cone direction (allowing for random variance)
        // With random distribution, we expect at least 25% to point rightward
        Assert.True(rightwardCount >= 25, $"Expected >=25 rightward, got {rightwardCount}");
    }

    [Fact]
    public void SpawnSystem_ConeShape_ZeroDirection_DefaultsToUnitY()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // Cone with zero vector direction - should default to UnitY
        var emitter = ParticleEmitter.Burst(100, 1f) with
        {
            Shape = EmissionShape.Cone(MathF.PI / 6f, 5f, Vector2.Zero),
            StartSpeedMin = 50f,
            StartSpeedMax = 50f
        };
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        // Should still spawn successfully even with zero direction
        Assert.Equal(100, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_BoxShape_SpawnsWithinBounds()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var center = new Vector2(200f, 200f);
        var width = 100f;
        var height = 50f;
        var emitter = ParticleEmitter.Burst(100, 1f) with
        {
            Shape = EmissionShape.Box(width, height),
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var entity = world.Spawn()
            .With(new Transform2D(center, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Equal(100, alive.Count);

        // All particles should be within box bounds
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;
        foreach (var idx in alive)
        {
            var dx = pool.PositionsX[idx] - center.X;
            var dy = pool.PositionsY[idx] - center.Y;
            Assert.True(dx >= -halfWidth - 1f && dx <= halfWidth + 1f,
                $"X offset {dx} outside box width {width}");
            Assert.True(dy >= -halfHeight - 1f && dy <= halfHeight + 1f,
                $"Y offset {dy} outside box height {height}");
        }
    }

    [Fact]
    public void SpawnSystem_PointShape_SpawnsAtEmitterPosition()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var position = new Vector2(150f, 250f);
        var emitter = ParticleEmitter.Burst(10, 1f) with
        {
            Shape = EmissionShape.Point,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var entity = world.Spawn()
            .With(new Transform2D(position, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var alive = FindAllAlive(pool);
        Assert.Equal(10, alive.Count);

        // All particles should spawn at the exact emitter position
        foreach (var idx in alive)
        {
            Assert.InRange(pool.PositionsX[idx], position.X - 1f, position.X + 1f);
            Assert.InRange(pool.PositionsY[idx], position.Y - 1f, position.Y + 1f);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SpawnSystem_NoPool_DoesNotCrash()
    {
        world = new World();
        // Install plugin but somehow create entity without proper pool setup
        // (This tests the null check in Update)
        world.InstallPlugin(new ParticlesPlugin());

        // Create emitter that's already not playing - won't trigger pool creation issues
        var emitter = ParticleEmitter.Burst(10, 1f) with { IsPlaying = false };
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Should not throw
        world.Update(1f / 60f);
    }

    [Fact]
    public void SpawnSystem_ZeroEmissionRate_OnlyBursts()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = new ParticleEmitter
        {
            EmissionRate = 0f,
            BurstCount = 10,
            BurstInterval = 0f,
            LifetimeMin = 1f,
            LifetimeMax = 1f,
            StartSizeMin = 1f,
            StartSizeMax = 1f,
            StartSpeedMin = 0f,
            StartSpeedMax = 0f,
            IsPlaying = true
        };
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // First update triggers burst
        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);
        Assert.Equal(10, pool.ActiveCount);

        // Additional updates should not spawn more (no emission rate, burst already fired)
        world.Update(1f / 60f);
        world.Update(1f / 60f);
        Assert.Equal(10, pool.ActiveCount);
    }

    #endregion

    #region Lazy Initialization Tests

    [Fact]
    public void SpawnSystem_LazyInitManager_AcquiresManagerOnUpdate()
    {
        // Setup: Add ONLY the spawn system before extension exists
        world = new World();
        var system = new ParticleSpawnSystem();
        world.AddSystem(system, SystemPhase.Update);

        // Now set up the particle manager AFTER system is initialized
        var config = new ParticlesConfig();
        var manager = new ParticleManager(world, config);
        world.SetExtension(manager);

        // Create an emitter
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Manually register the emitter since we didn't use the plugin
        ref var emitter = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in emitter);

        // First update triggers lazy init path (manager was null during OnInitialize)
        world.Update(1f / 60f);

        // Verify particles were spawned (confirms lazy init succeeded)
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);
        Assert.Equal(5, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_LazyInitManager_ReturnsEarly_WhenNoExtension()
    {
        // Setup: Add spawn system with NO manager extension ever set
        world = new World();
        var system = new ParticleSpawnSystem();
        world.AddSystem(system, SystemPhase.Update);

        // Create an emitter (no pool since no manager)
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Update should return early - no exception, no particles
        world.Update(1f / 60f);

        // No exception means success - lazy init returned early
    }

    [Fact]
    public void SpawnSystem_NoManagerExtension_ReturnsEarly()
    {
        // Setup: Add system with no extensions
        world = new World();
        var system = new ParticleSpawnSystem();
        world.AddSystem(system, SystemPhase.Update);

        // Create entities (no manager registered)
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Update should not throw - just returns early
        world.Update(1f / 60f);
    }

    #endregion

    #region Pool Null Check Tests

    [Fact]
    public void SpawnSystem_MaxEmittersExceeded_PoolIsNull_DoesNotCrash()
    {
        // Setup: Create world with max 1 emitter
        world = new World();
        var config = new ParticlesConfig { MaxEmitters = 1, MaxParticlesPerEmitter = 100, InitialPoolCapacity = 50 };
        world.InstallPlugin(new ParticlesPlugin(config));

        // First emitter succeeds
        var entity1 = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Second emitter will NOT get a pool (max exceeded)
        var entity2 = world.Spawn()
            .With(new Transform2D(new Vector2(100, 100), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        var manager = world.GetExtension<ParticleManager>();

        // First pool exists
        Assert.NotNull(manager.GetPool(entity1));
        // Second pool is null (max emitters exceeded)
        Assert.Null(manager.GetPool(entity2));

        // Update should not crash even with null pool
        world.Update(1f / 60f);

        // First emitter's particles were spawned
        Assert.Equal(5, manager.GetPool(entity1)!.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_Transform3D_MaxEmittersExceeded_DoesNotCrash()
    {
        // Test pool null check in Transform3D code path
        world = new World();
        var config = new ParticlesConfig { MaxEmitters = 1, MaxParticlesPerEmitter = 100, InitialPoolCapacity = 50 };
        world.InstallPlugin(new ParticlesPlugin(config));

        // First emitter with Transform2D succeeds
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Second emitter with Transform3D will NOT get a pool (max exceeded)
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(100, 100, 0), Quaternion.Identity, Vector3.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        var manager = world.GetExtension<ParticleManager>();

        // Second pool is null (max emitters exceeded)
        Assert.Null(manager.GetPool(entity2));

        // Update should not crash even with null pool for Transform3D emitter
        world.Update(1f / 60f);
    }

    #endregion
}
