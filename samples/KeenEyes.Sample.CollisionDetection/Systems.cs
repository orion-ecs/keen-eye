using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.CollisionDetection;

/// <summary>
/// Class for tracking collision statistics.
/// </summary>
public class CollisionStats
{
    /// <summary>Total number of confirmed collisions.</summary>
    public int TotalCollisions;

    /// <summary>Number of broadphase candidate pair checks.</summary>
    public int BroadphaseChecks;

    /// <summary>Number of narrowphase exact collision tests.</summary>
    public int NarrowphaseChecks;
}

/// <summary>
/// System that moves entities based on their velocity.
/// </summary>
public class MovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);

            // Move entity
            transform.Position += velocity.Value * deltaTime;

            // Wrap around world bounds (simple toroidal world)
            const float halfSize = Program.WorldSize / 2;
            if (transform.Position.X < -halfSize)
            {
                transform.Position.X += Program.WorldSize;
            }

            if (transform.Position.X > halfSize)
            {
                transform.Position.X -= Program.WorldSize;
            }

            if (transform.Position.Z < -halfSize)
            {
                transform.Position.Z += Program.WorldSize;
            }

            if (transform.Position.Z > halfSize)
            {
                transform.Position.Z -= Program.WorldSize;
            }
        }
    }
}

/// <summary>
/// System that detects collisions using spatial partitioning (broadphase + narrowphase).
/// </summary>
public class SpatialCollisionSystem(CollisionStats stats) : SystemBase
{
    private SpatialQueryApi? spatial;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Check collisions for all entities
        foreach (var entity in World.Query<Transform3D, CollisionRadius>())
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var radius = ref World.Get<CollisionRadius>(entity);

            // Broadphase: query nearby entities using spatial index
            foreach (var other in spatial!.QueryRadius(transform.Position, radius.Value * 2))
            {
                if (other.Id <= entity.Id)
                {
                    continue;  // Skip self and avoid duplicate pairs
                }

                stats.BroadphaseChecks++;

                // Narrowphase: exact sphere-sphere collision test
                ref readonly var otherTransform = ref World.Get<Transform3D>(other);
                ref readonly var otherRadius = ref World.Get<CollisionRadius>(other);

                float combinedRadius = radius.Value + otherRadius.Value;
                float distSq = Vector3.DistanceSquared(transform.Position, otherTransform.Position);

                stats.NarrowphaseChecks++;

                if (distSq <= combinedRadius * combinedRadius)
                {
                    // Collision detected!
                    stats.TotalCollisions++;
                    HandleCollision(entity, other);
                }
            }
        }
    }

    private void HandleCollision(Entity a, Entity b)
    {
        // In a real game, you might:
        // - Trigger events
        // - Apply physics forces
        // - Deal damage
        // - Play sound effects
        // For this sample, we just count collisions
    }
}

/// <summary>
/// System that detects collisions using naive O(n²) approach (no spatial partitioning).
/// </summary>
public class NaiveCollisionSystem(CollisionStats stats) : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Get all entities (inefficient!)
#pragma warning disable KEEN031 // Intentionally inefficient for performance comparison
        var entities = World.Query<Transform3D, CollisionRadius>().ToList();
#pragma warning restore KEEN031

        // Check every pair (O(n²))
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var radius = ref World.Get<CollisionRadius>(entity);

            for (int j = i + 1; j < entities.Count; j++)
            {
                var other = entities[j];
                stats.BroadphaseChecks++;  // Count all pair checks

                ref readonly var otherTransform = ref World.Get<Transform3D>(other);
                ref readonly var otherRadius = ref World.Get<CollisionRadius>(other);

                float combinedRadius = radius.Value + otherRadius.Value;
                float distSq = Vector3.DistanceSquared(transform.Position, otherTransform.Position);

                if (distSq <= combinedRadius * combinedRadius)
                {
                    // Collision detected!
                    stats.TotalCollisions++;
                    HandleCollision(entity, other);
                }
            }
        }
    }

    private void HandleCollision(Entity a, Entity b)
    {
        // For this sample, we just count collisions
    }
}
