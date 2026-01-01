namespace KeenEyes.Sample.MassSimulation;

// =============================================================================
// SYSTEMS - Mass Entity Simulation
// =============================================================================

/// <summary>
/// Updates particle positions based on velocity.
/// </summary>
public class MovementSystem : SystemBase
{
    /// <summary>World bounds width.</summary>
    public float WorldWidth { get; set; } = 100f;

    /// <summary>World bounds height.</summary>
    public float WorldHeight { get; set; } = 50f;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>().Without<Dead>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;

            // Wrap around world bounds
            if (pos.X < 0)
            {
                pos.X += WorldWidth;
            }

            if (pos.X >= WorldWidth)
            {
                pos.X -= WorldWidth;
            }

            if (pos.Y < 0)
            {
                pos.Y += WorldHeight;
            }

            if (pos.Y >= WorldHeight)
            {
                pos.Y -= WorldHeight;
            }
        }
    }
}

/// <summary>
/// Applies gravity to particles with the Gravity component.
/// </summary>
public class GravitySystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Velocity, Gravity>().Without<Dead>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            ref readonly var gravity = ref World.Get<Gravity>(entity);

            vel.Y += gravity.Strength * deltaTime;
        }
    }
}

/// <summary>
/// Updates particle lifetimes and marks expired particles as dead.
/// </summary>
public class LifetimeSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Lifetime>().Without<Dead>())
        {
            ref var lifetime = ref World.Get<Lifetime>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.Remaining <= 0)
            {
                World.Add(entity, new Dead());
            }
        }
    }
}

/// <summary>
/// Removes dead particles from the world.
/// </summary>
public class CleanupSystem : SystemBase
{
    /// <summary>Number of entities cleaned up in the last update.</summary>
    public int LastCleanupCount { get; private set; }

    // Pre-allocated list to avoid per-frame allocations
    private readonly List<Entity> toRemove = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        toRemove.Clear();

        foreach (var entity in World.Query<Position>().With<Dead>())
        {
            toRemove.Add(entity);
        }

        foreach (var entity in toRemove)
        {
            World.Despawn(entity);
        }

        LastCleanupCount = toRemove.Count;
    }
}

/// <summary>
/// Spawns new particles to maintain target entity count.
/// </summary>
/// <remarks>
/// Uses World.NextFloat() for random number generation, ensuring deterministic behavior
/// when the world is seeded. This is important for replay systems and testing.
/// </remarks>
public class SpawnerSystem : SystemBase
{
    /// <summary>Target number of active particles.</summary>
    public int TargetCount { get; set; } = 100_000;

    /// <summary>Maximum particles to spawn per frame.</summary>
    public int MaxSpawnPerFrame { get; set; } = 5_000;

    /// <summary>World width for spawn positions.</summary>
    public float WorldWidth { get; set; } = 100f;

    /// <summary>World height for spawn positions.</summary>
    public float WorldHeight { get; set; } = 50f;

    /// <summary>Number of entities spawned in the last update.</summary>
    public int LastSpawnCount { get; private set; }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var currentCount = World.Query<Position>().Without<Dead>().Count();
        var toSpawn = Math.Min(TargetCount - currentCount, MaxSpawnPerFrame);

        if (toSpawn <= 0)
        {
            LastSpawnCount = 0;
            return;
        }

        for (var i = 0; i < toSpawn; i++)
        {
            var hasGravity = World.NextBool(0.3f); // 30% chance of gravity

            var builder = World.Spawn()
                .WithPosition(
                    x: World.NextFloat() * WorldWidth,
                    y: World.NextFloat() * WorldHeight)
                .WithVelocity(
                    x: World.NextFloat() * 20 - 10,
                    y: World.NextFloat() * 20 - 10)
                .WithLifetime(remaining: World.NextFloat() * 5 + 1)
                .WithParticleColor(hue: World.NextFloat() * 360)
                .WithActive();

            if (hasGravity)
            {
                builder.WithGravity(strength: 9.8f);
            }

            builder.Build();
        }

        LastSpawnCount = toSpawn;
    }
}
