using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;
using KeenEyes.Particles.Systems;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Integration tests for particle systems.
/// </summary>
public class ParticleSystemIntegrationTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    /// <summary>
    /// Finds the first alive particle index in the pool.
    /// </summary>
    private static int FindFirstAlive(ParticlePool pool)
    {
        for (var i = 0; i < pool.Capacity; i++)
        {
            if (pool.Alive[i])
            {
                return i;
            }
        }
        return -1;
    }

    #region Spawn System Tests

    [Fact]
    public void SpawnSystem_ContinuousEmission_SpawnsParticlesOverTime()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Continuous(60f, 1f); // 60 particles per second
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);
        Assert.Equal(0, pool.ActiveCount);

        // Run update for 1 second (in 60 small steps of ~16ms each)
        for (var i = 0; i < 60; i++)
        {
            world.Update(1f / 60f); // ~16.67ms per frame
        }

        // Should have spawned approximately 60 particles
        Assert.InRange(pool.ActiveCount, 50, 70);
    }

    [Fact]
    public void SpawnSystem_BurstEmission_SpawnsAllAtOnce()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(100, 2f); // 100 particles, 2 second lifetime
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Single update should trigger burst
        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.Equal(100, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_NotPlaying_DoesNotSpawn()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Continuous(100f, 1f) with { IsPlaying = false };
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        // Run update for 1 second
        for (var i = 0; i < 60; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void SpawnSystem_ParticlesHaveCorrectInitialProperties()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var startColor = new Vector4(1f, 0.5f, 0.25f, 0.8f);
        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartColor = startColor,
            StartSizeMin = 10f,
            StartSizeMax = 10f,
            StartSpeedMin = 50f,
            StartSpeedMax = 50f
        };

        var position = new Vector2(100f, 200f);
        var entity = world.Spawn()
            .With(new Transform2D(position, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        Assert.Equal(1, pool.ActiveCount);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);

        // Check color
        Assert.Equal(startColor.X, pool.ColorsR[idx]);
        Assert.Equal(startColor.Y, pool.ColorsG[idx]);
        Assert.Equal(startColor.Z, pool.ColorsB[idx]);
        Assert.Equal(startColor.W, pool.ColorsA[idx]);

        // Check size
        Assert.Equal(10f, pool.Sizes[idx]);
        Assert.Equal(10f, pool.InitialSizes[idx]);

        // Age is ~0 after spawn (but update system already ran, so it's one frame)
        Assert.True(pool.Ages[idx] < 0.02f);
    }

    [Fact]
    public void SpawnSystem_PointShape_SpawnsNearEmitterPosition()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            Shape = EmissionShape.Point
        };

        var position = new Vector2(50f, 75f);
        var entity = world.Spawn()
            .With(new Transform2D(position, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);

        Assert.NotNull(pool);
        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);

        // Point shape should spawn near emitter position (may have moved slightly due to velocity)
        Assert.InRange(pool.PositionsX[idx], position.X - 5f, position.X + 5f);
        Assert.InRange(pool.PositionsY[idx], position.Y - 5f, position.Y + 5f);
    }

    #endregion

    #region Update System Tests

    [Fact]
    public void UpdateSystem_ParticlesMove()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f) with
        {
            StartSpeedMin = 100f,
            StartSpeedMax = 100f,
            Shape = EmissionShape.Cone(0f, 0f, new Vector2(1f, 0f)) // Emit to the right
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f); // Spawn

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        var initialX = pool.PositionsX[idx];

        // Run a few more updates
        for (var i = 0; i < 6; i++)
        {
            world.Update(1f / 60f);
        }

        // Particle should have moved
        Assert.True(pool.PositionsX[idx] != initialX);
    }

    [Fact]
    public void UpdateSystem_ParticlesAge()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f);
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f); // Spawn

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        // Age is ~1 frame after spawn (update system already ran)
        Assert.True(pool.Ages[idx] < 0.02f);

        // Run updates for ~500ms (30 frames at 60fps)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        Assert.True(pool.Ages[idx] > 0.4f);
        Assert.True(pool.NormalizedAges[idx] > 0);
    }

    [Fact]
    public void UpdateSystem_DeadParticlesAreReleased()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(10, 0.5f); // 0.5 second lifetime
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);
        Assert.Equal(10, pool.ActiveCount);

        // Wait for particles to die (~36 frames for 600ms)
        for (var i = 0; i < 40; i++)
        {
            world.Update(1f / 60f);
        }

        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void UpdateSystem_GravityModifier_AppliesGravity()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f) with
        {
            StartSpeedMin = 0f,
            StartSpeedMax = 0f
        };
        var modifiers = new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityY = 100f // Downward
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        var initialY = pool.PositionsY[idx];
        var initialVelY = pool.VelocitiesY[idx];

        // Run updates for ~100ms (6 frames)
        for (var i = 0; i < 6; i++)
        {
            world.Update(1f / 60f);
        }

        // Velocity should have increased due to gravity
        Assert.True(pool.VelocitiesY[idx] > initialVelY);
        // Position should have moved down
        Assert.True(pool.PositionsY[idx] > initialY);
    }

    [Fact]
    public void UpdateSystem_ColorOverLifetime_ChangesColor()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartColor = new Vector4(1f, 1f, 1f, 1f)
        };
        var modifiers = new ParticleEmitterModifiers
        {
            HasColorOverLifetime = true,
            ColorGradient = ParticleGradient.FadeOut(new Vector4(1f, 1f, 1f, 1f))
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        var initialAlpha = pool.ColorsA[idx];
        // Initial alpha is close to 1 (may have started fading already after first frame)
        Assert.True(initialAlpha > 0.95f);

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        // Alpha should have decreased
        Assert.True(pool.ColorsA[idx] < initialAlpha);
    }

    [Fact]
    public void UpdateSystem_SizeOverLifetime_ChangesSize()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 1f) with
        {
            StartSizeMin = 10f,
            StartSizeMax = 10f
        };
        var modifiers = new ParticleEmitterModifiers
        {
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut()
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        var initialSize = pool.Sizes[idx];

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        // Size should have decreased
        Assert.True(pool.Sizes[idx] < initialSize);
    }

    #endregion

    #region Full Effect Integration Tests

    [Fact]
    public void FireEffect_ProducesParticles()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        world.Spawn()
            .With(new Transform2D(new Vector2(100f, 100f), 0f, Vector2.One))
            .With(ParticleEffects.Fire())
            .With(ParticleEffects.FireModifiers())
            .Build();

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        Assert.True(manager.TotalActiveParticles > 0);
    }

    [Fact]
    public void ExplosionEffect_BurstsAndFades()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEffects.Explosion())
            .With(ParticleEffects.ExplosionModifiers())
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var initialCount = manager.TotalActiveParticles;
        Assert.Equal(100, initialCount); // Explosion bursts 100

        // Wait for explosion to fade (~90 frames for 1.5 seconds)
        for (var i = 0; i < 100; i++)
        {
            world.Update(1f / 60f);
        }

        Assert.True(manager.TotalActiveParticles < initialCount);
    }

    #endregion

    #region VelocityOverLifetime Tests

    [Fact]
    public void UpdateSystem_VelocityOverLifetime_SlowsParticle()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f) with
        {
            StartSpeedMin = 100f,
            StartSpeedMax = 100f,
            Shape = EmissionShape.Cone(0f, 0f, new Vector2(1f, 0f)) // Emit to the right
        };
        var modifiers = new ParticleEmitterModifiers
        {
            HasVelocityOverLifetime = true,
            VelocityCurve = ParticleCurve.LinearFadeOut() // Slow down over time
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        // Velocity should have decreased significantly due to curve
        // Note: velocity is multiplied by the curve value each frame
        Assert.True(Math.Abs(pool.VelocitiesX[idx]) < 50f);
    }

    #endregion

    #region RotationOverLifetime Tests

    [Fact]
    public void UpdateSystem_RotationOverLifetime_RotatesParticle()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f);
        var modifiers = new ParticleEmitterModifiers
        {
            HasRotationOverLifetime = true,
            RotationSpeed = MathF.PI, // 180 degrees per second
            RotationCurve = ParticleCurve.Constant(1f)
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);
        var initialRotation = pool.Rotations[idx];

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        // Rotation should have increased
        Assert.True(pool.Rotations[idx] > initialRotation);
        // Should be approximately PI/2 after 0.5 seconds at PI rad/s
        Assert.InRange(pool.Rotations[idx], MathF.PI / 4f, MathF.PI);
    }

    [Fact]
    public void UpdateSystem_RotationOverLifetime_WithCurve_ScalesSpeed()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 1f);
        var modifiers = new ParticleEmitterModifiers
        {
            HasRotationOverLifetime = true,
            RotationSpeed = MathF.PI * 2, // Full rotation per second
            RotationCurve = ParticleCurve.LinearFadeOut() // Slow down over time
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);

        // Run updates for ~500ms (30 frames)
        for (var i = 0; i < 30; i++)
        {
            world.Update(1f / 60f);
        }

        // Rotation speed should have decreased due to curve
        Assert.True(pool.RotationSpeeds[idx] < MathF.PI * 2);
    }

    #endregion

    #region Drag Edge Cases

    [Fact]
    public void UpdateSystem_HighDrag_ClampsToZero()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var emitter = ParticleEmitter.Burst(1, 2f) with
        {
            StartSpeedMin = 100f,
            StartSpeedMax = 100f,
            Shape = EmissionShape.Cone(0f, 0f, new Vector2(1f, 0f))
        };
        var modifiers = new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityY = 0f,
            Drag = 100f // Very high drag - will cause dragFactor < 0
        };

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .Build();

        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);

        var idx = FindFirstAlive(pool);
        Assert.True(idx >= 0);

        // With very high drag, velocity should be clamped to 0
        world.Update(1f / 60f);

        // Velocity should be 0 due to extreme drag clamping
        Assert.Equal(0f, pool.VelocitiesX[idx], 4);
        Assert.Equal(0f, pool.VelocitiesY[idx], 4);
    }

    #endregion

    #region Component Construction Tests

    [Fact]
    public void ParticleEmitter_CustomConfiguration_Works()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(new ParticleEmitter
            {
                EmissionRate = 50f,
                BurstCount = 0,
                BurstInterval = 0f,
                LifetimeMin = 1f,
                LifetimeMax = 2f,
                StartSizeMin = 5f,
                StartSizeMax = 10f,
                StartSpeedMin = 20f,
                StartSpeedMax = 40f,
                StartRotationMin = 0f,
                StartRotationMax = MathF.PI,
                Shape = EmissionShape.Point,
                Texture = default,
                StartColor = new Vector4(1f, 1f, 1f, 1f),
                BlendMode = BlendMode.Transparent,
                IsPlaying = true
            })
            .Build();

        Assert.True(world.Has<ParticleEmitter>(entity));

        // Run a few frames
        for (var i = 0; i < 10; i++)
        {
            world.Update(1f / 60f);
        }

        var manager = world.GetExtension<ParticleManager>();
        Assert.True(manager.TotalActiveParticles > 0);
    }

    [Fact]
    public void ParticleEmitterModifiers_AllModifiers_Works()
    {
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(10, 1f))
            .With(new ParticleEmitterModifiers
            {
                HasGravity = true,
                GravityX = 0f,
                GravityY = 100f,
                Drag = 0.1f,
                HasVelocityOverLifetime = false,
                VelocityCurve = ParticleCurve.Constant(1f),
                HasSizeOverLifetime = true,
                SizeCurve = ParticleCurve.LinearFadeOut(),
                HasColorOverLifetime = true,
                ColorGradient = ParticleGradient.FadeOut(new Vector4(1f, 1f, 1f, 1f)),
                HasRotationOverLifetime = false,
                RotationSpeed = 0f,
                RotationCurve = ParticleCurve.Constant(1f)
            })
            .Build();

        Assert.True(world.Has<ParticleEmitter>(entity));
        Assert.True(world.Has<ParticleEmitterModifiers>(entity));

        var modifiers = world.Get<ParticleEmitterModifiers>(entity);
        Assert.True(modifiers.HasGravity);
        Assert.Equal(100f, modifiers.GravityY);
        Assert.True(modifiers.HasSizeOverLifetime);
        Assert.True(modifiers.HasColorOverLifetime);
    }

    #endregion

    #region UpdateSystem Lazy Initialization Tests

    [Fact]
    public void UpdateSystem_LazyInitManager_AcquiresManagerOnUpdate()
    {
        // Setup: Add system before extension exists
        world = new World();
        var system = new ParticleUpdateSystem();
        world.AddSystem(system, SystemPhase.Update);

        // Now set up the particle manager AFTER system is initialized
        var config = new ParticlesConfig();
        var manager = new ParticleManager(world, config);
        world.SetExtension(manager);

        // Create an emitter and manually spawn a particle
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(1, 1f))
            .Build();

        ref var emitter = ref world.Get<ParticleEmitter>(entity);
        manager.RegisterEmitter(entity, in emitter);

        var pool = manager.GetPool(entity)!;
        var idx = pool.Allocate();
        pool.Alive[idx] = true;
        pool.Ages[idx] = 0f;
        pool.Lifetimes[idx] = 1f;
        pool.VelocitiesX[idx] = 100f;

        // First update triggers lazy init path
        var initialX = pool.PositionsX[idx];
        world.Update(1f / 60f);

        // Verify particle was updated (moved)
        Assert.True(pool.PositionsX[idx] > initialX);
    }

    [Fact]
    public void UpdateSystem_NoManagerExtension_ReturnsEarly()
    {
        // Setup: Add system with no extensions
        world = new World();
        var system = new ParticleUpdateSystem();
        world.AddSystem(system, SystemPhase.Update);

        // Create entities (no manager registered)
        world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Update should not throw - just returns early
        world.Update(1f / 60f);
    }

    [Fact]
    public void UpdateSystem_PoolNull_SkipsProcessing()
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
        world.Spawn()
            .With(new Transform2D(new Vector2(100, 100), 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f))
            .Build();

        // Update should not crash - update system skips null pools
        world.Update(1f / 60f);

        // First pool particles were updated
        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity1);
        Assert.NotNull(pool);
    }

    [Fact]
    public void UpdateSystem_EmptyPool_SkipsProcessing()
    {
        // Test the ActiveCount == 0 check
        world = new World();
        world.InstallPlugin(new ParticlesPlugin());

        // Emitter that's not playing - pool will be empty
        var entity = world.Spawn()
            .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
            .With(ParticleEmitter.Burst(5, 1f) with { IsPlaying = false })
            .Build();

        // Update should not crash - update system skips empty pools
        world.Update(1f / 60f);

        var manager = world.GetExtension<ParticleManager>();
        var pool = manager.GetPool(entity);
        Assert.NotNull(pool);
        Assert.Equal(0, pool.ActiveCount);
    }

    #endregion
}
