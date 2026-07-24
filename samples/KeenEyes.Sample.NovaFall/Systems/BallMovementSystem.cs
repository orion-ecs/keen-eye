using KeenEyes.Common;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Integrates ball motion: gravity, steering acceleration, horizontal speed clamp,
/// and wall clamping at the shaft edges.
/// </summary>
/// <remarks>
/// Vertical integration is skipped while the ball rests on a floor —
/// <see cref="CollisionSystem"/> keeps the resting ball glued to the floor that
/// carries it upward. Horizontal steering always applies, which is how the player
/// slides a resting ball toward the gap.
/// </remarks>
public sealed class BallMovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        var dt = deltaTime * World.GetSingleton<TimeScale>().Value;

        foreach (var entity in World.Query<Ball, Position2D, Velocity2D, SteerInput>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var steer = ref World.Get<SteerInput>(entity);
            ref var position = ref World.Get<Position2D>(entity);
            ref var velocity = ref World.Get<Velocity2D>(entity);

            var resting = World.Has<RestingOn>(entity);

            // Horizontal: accelerate with input, damp toward zero without it.
            // Tolerance-based comparison — never == on floats.
            if (!steer.Axis.IsApproximatelyZero())
            {
                velocity.X += steer.Axis * Tuning.SteerAcceleration * dt;
            }
            else
            {
                var damping = resting ? Tuning.GroundFriction : Tuning.AirDrag;
                velocity.X -= velocity.X * Math.Min(damping * dt, 1f);
            }

            velocity.X = Math.Clamp(velocity.X, -Tuning.MaxHorizontalSpeed, Tuning.MaxHorizontalSpeed);
            position.X += velocity.X * dt;

            // Wall clamp: the shaft has hard edges.
            var minX = ball.Radius;
            var maxX = Tuning.ShaftWidth - ball.Radius;
            if (position.X < minX)
            {
                position.X = minX;
                velocity.X = 0f;
            }
            else if (position.X > maxX)
            {
                position.X = maxX;
                velocity.X = 0f;
            }

            // Vertical: gravity only while airborne.
            if (!resting)
            {
                velocity.Y = Math.Min(velocity.Y + Tuning.Gravity * dt, Tuning.MaxFallSpeed);
                position.Y += velocity.Y * dt;
            }
        }
    }
}
