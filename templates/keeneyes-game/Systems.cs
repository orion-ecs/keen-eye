using KeenEyes;

namespace KeenEyesGame;

/// <summary>
/// Moves entities by applying velocity to position.
/// </summary>
[System(Phase = SystemPhase.Update, Order = 0)]
public partial class MovementSystem : SystemBase
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
/// Renders entities (placeholder for actual rendering logic).
/// </summary>
[System(Phase = SystemPhase.Render, Order = 0)]
public partial class RenderSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            Console.WriteLine($"  Render {entity} at ({pos.X:F2}, {pos.Y:F2})");
        }
    }
}
