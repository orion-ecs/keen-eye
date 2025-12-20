using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Systems;

/// <summary>
/// System that spawns new particles from emitters.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the Update phase and handles both continuous emission
/// (based on <see cref="ParticleEmitter.EmissionRate"/>) and burst emission
/// (based on <see cref="ParticleEmitter.BurstCount"/>).
/// </para>
/// <para>
/// Spawned particles are placed in the emitter's particle pool with random
/// initial properties within the configured min/max ranges.
/// </para>
/// </remarks>
public sealed class ParticleSpawnSystem : SystemBase
{
    private ParticleManager? manager;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<ParticleManager>(out var pm))
        {
            manager = pm;
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var pm = manager;
        if (pm == null)
        {
            if (!World.TryGetExtension(out pm) || pm is null)
            {
                return;
            }
            manager = pm;
        }

        // Query all emitters with Transform2D
        foreach (var entity in World.Query<ParticleEmitter, Transform2D>())
        {
            ref var emitter = ref World.Get<ParticleEmitter>(entity);
            if (!emitter.IsPlaying)
            {
                continue;
            }

            ref readonly var transform = ref World.Get<Transform2D>(entity);
            var pool = pm.GetPool(entity);
            if (pool == null)
            {
                continue;
            }

            var toSpawn = 0;

            // Continuous emission
            if (emitter.EmissionRate > 0)
            {
                emitter.EmissionAccumulator += emitter.EmissionRate * deltaTime;
                var continuousSpawn = (int)emitter.EmissionAccumulator;
                emitter.EmissionAccumulator -= continuousSpawn;
                toSpawn += continuousSpawn;
            }

            // Burst emission
            if (emitter.BurstCount > 0)
            {
                if (emitter.BurstInterval <= 0)
                {
                    // One-shot burst
                    if (!emitter.InitialBurstEmitted)
                    {
                        toSpawn += emitter.BurstCount;
                        emitter.InitialBurstEmitted = true;
                    }
                }
                else
                {
                    // Repeating burst
                    emitter.BurstTimer += deltaTime;
                    while (emitter.BurstTimer >= emitter.BurstInterval)
                    {
                        toSpawn += emitter.BurstCount;
                        emitter.BurstTimer -= emitter.BurstInterval;
                    }
                }
            }

            // Spawn particles
            for (var i = 0; i < toSpawn; i++)
            {
                SpawnParticle(pool, in emitter, in transform, pm.Config.MaxParticlesPerEmitter);
            }
        }

        // Also query emitters with Transform3D (project to 2D)
        foreach (var entity in World.Query<ParticleEmitter, Transform3D>())
        {
            // Skip if also has Transform2D (already processed)
            if (World.Has<Transform2D>(entity))
            {
                continue;
            }

            ref var emitter = ref World.Get<ParticleEmitter>(entity);
            if (!emitter.IsPlaying)
            {
                continue;
            }

            ref readonly var transform3D = ref World.Get<Transform3D>(entity);
            var pool = pm.GetPool(entity);
            if (pool == null)
            {
                continue;
            }

            // Project to 2D
            var transform = new Transform2D(
                new Vector2(transform3D.Position.X, transform3D.Position.Y),
                0f,
                new Vector2(transform3D.Scale.X, transform3D.Scale.Y));

            var toSpawn = 0;

            // Continuous emission
            if (emitter.EmissionRate > 0)
            {
                emitter.EmissionAccumulator += emitter.EmissionRate * deltaTime;
                var continuousSpawn = (int)emitter.EmissionAccumulator;
                emitter.EmissionAccumulator -= continuousSpawn;
                toSpawn += continuousSpawn;
            }

            // Burst emission
            if (emitter.BurstCount > 0)
            {
                if (emitter.BurstInterval <= 0)
                {
                    if (!emitter.InitialBurstEmitted)
                    {
                        toSpawn += emitter.BurstCount;
                        emitter.InitialBurstEmitted = true;
                    }
                }
                else
                {
                    emitter.BurstTimer += deltaTime;
                    while (emitter.BurstTimer >= emitter.BurstInterval)
                    {
                        toSpawn += emitter.BurstCount;
                        emitter.BurstTimer -= emitter.BurstInterval;
                    }
                }
            }

            for (var i = 0; i < toSpawn; i++)
            {
                SpawnParticle(pool, in emitter, in transform, pm.Config.MaxParticlesPerEmitter);
            }
        }
    }

    private void SpawnParticle(ParticlePool pool, in ParticleEmitter emitter, in Transform2D transform, int maxParticles)
    {
        var index = pool.Allocate();
        if (index < 0)
        {
            // Pool full, try to grow
            pool.Grow(pool.Capacity * 2, maxParticles);
            index = pool.Allocate();
            if (index < 0)
            {
                return; // Still full
            }
        }

        // Calculate spawn position and direction based on shape
        var (posOffset, direction) = CalculateSpawnPosition(emitter.Shape);

        pool.PositionsX[index] = transform.Position.X + posOffset.X;
        pool.PositionsY[index] = transform.Position.Y + posOffset.Y;

        // Calculate velocity
        var speed = Lerp(emitter.StartSpeedMin, emitter.StartSpeedMax, World.NextFloat());
        pool.VelocitiesX[index] = direction.X * speed;
        pool.VelocitiesY[index] = direction.Y * speed;

        // Set visual properties
        pool.ColorsR[index] = emitter.StartColor.X;
        pool.ColorsG[index] = emitter.StartColor.Y;
        pool.ColorsB[index] = emitter.StartColor.Z;
        pool.ColorsA[index] = emitter.StartColor.W;

        var size = Lerp(emitter.StartSizeMin, emitter.StartSizeMax, World.NextFloat());
        pool.Sizes[index] = size;
        pool.InitialSizes[index] = size;

        pool.Rotations[index] = Lerp(emitter.StartRotationMin, emitter.StartRotationMax, World.NextFloat());
        pool.RotationSpeeds[index] = 0;

        // Set lifecycle
        pool.Ages[index] = 0;
        pool.Lifetimes[index] = Lerp(emitter.LifetimeMin, emitter.LifetimeMax, World.NextFloat());
        pool.NormalizedAges[index] = 0;
    }

    private (Vector2 Position, Vector2 Direction) CalculateSpawnPosition(EmissionShape shape)
    {
        switch (shape.Type)
        {
            case EmissionShapeType.Point:
                return (Vector2.Zero, RandomDirection());

            case EmissionShapeType.Sphere:
                var sphereDir = RandomDirection();
                var dist = World.NextFloat() * shape.Radius;
                return (sphereDir * dist, sphereDir);

            case EmissionShapeType.Cone:
                var baseDir = shape.Direction;
                if (baseDir == Vector2.Zero)
                {
                    baseDir = Vector2.UnitY;
                }
                var baseAngle = MathF.Atan2(baseDir.Y, baseDir.X);
                var halfAngle = shape.Angle / 2f;
                var angle = baseAngle + Lerp(-halfAngle, halfAngle, World.NextFloat());
                var coneDir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var coneDist = World.NextFloat() * shape.Radius;
                return (coneDir * coneDist, coneDir);

            case EmissionShapeType.Box:
                var x = Lerp(-shape.Size.X / 2, shape.Size.X / 2, World.NextFloat());
                var y = Lerp(-shape.Size.Y / 2, shape.Size.Y / 2, World.NextFloat());
                return (new Vector2(x, y), RandomDirection());

            default:
                return (Vector2.Zero, Vector2.UnitY);
        }
    }

    private Vector2 RandomDirection()
    {
        var angle = World.NextFloat() * MathF.PI * 2f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
