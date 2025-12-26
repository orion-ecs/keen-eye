namespace KeenEyes.Sample.Replay;

/// <summary>
/// System that moves entities based on their velocity.
/// </summary>
public sealed class MovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}

/// <summary>
/// System that simulates combat between entities.
/// </summary>
public sealed class CombatSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Simulate occasional damage to enemies
        foreach (var entity in World.Query<Health>().With<Enemy>())
        {
            if (World.NextDouble() < 0.1) // 10% chance per frame
            {
                ref var health = ref World.Get<Health>(entity);
                health.Current -= 5;

                if (health.Current <= 0)
                {
                    World.Despawn(entity);
                }
            }
        }
    }
}
