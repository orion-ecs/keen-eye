namespace KeenEyes.Sample.Aot;

// ============================================================================
// System Definitions (AOT-compatible)
// ============================================================================

/// <summary>System that updates entity positions based on velocity.</summary>
public class MovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.Dx * deltaTime;
            pos.Y += vel.Dy * deltaTime;
        }
    }
}

/// <summary>System that regenerates health for non-enemy entities.</summary>
public class HealthRegenSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);

            // Regenerate 1 health per second for non-enemies
            if (!World.Has<EnemyTag>(entity) && health.Current < health.Max)
            {
                health.Current = Math.Min(health.Current + (int)(10 * deltaTime), health.Max);
            }
        }
    }
}
