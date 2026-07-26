using KeenEyes.Common;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Integrates ball motion: gravity, steering acceleration, horizontal speed clamp,
/// and clamping to the shaft edges — the side walls and the shaft floor.
/// </summary>
/// <remarks>
/// <para>
/// Vertical integration is skipped while the ball rests on a floor —
/// <see cref="CollisionSystem"/> keeps the resting ball glued to the floor that
/// carries it upward. Horizontal steering always applies, which is how the player
/// slides a resting ball toward the gap.
/// </para>
/// <para>
/// TEACHING NOTE — the play field is a box, and every side of it must be a wall.
/// The ball falls faster (<see cref="Tuning.MaxFallSpeed"/>) than the shaft rises
/// (<see cref="Tuning.MaxScrollSpeed"/>), so a player who threads several gaps in
/// a row genuinely outruns the shaft. Without the shaft-floor clamp below, such a
/// run leaves the play field entirely: no floor can ever be spawned deep enough
/// to reach it again, no collision can fire, and — because depth (and therefore
/// score) accrues from the scroll speed rather than from the ball — the run
/// becomes unloseable with an ever-climbing score. Clamping instead of killing is
/// deliberate: NOVAFALL's fantasy is that falling is safety, so the original
/// TI-83 behaviour is the right one — the ball rides the bottom of the shaft and
/// the rising floors sweep up into it, which is self-correcting and creates
/// tension rather than punishing good play.
/// </para>
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

            // Shaft-floor clamp: the same idea as the wall clamp above, applied to
            // the bottom edge of the play field. The bound mirrors the horizontal
            // one exactly — the shaft's extent minus the ball's radius — so the
            // ball comes to rest with its lowest point on the bottom edge.
            //
            // Applied outside the airborne branch on purpose: this is an
            // invariant of the ball, not a step of the fall integration.
            var maxY = Tuning.ShaftHeight - ball.Radius;
            if (position.Y > maxY)
            {
                position.Y = maxY;

                // Rest, don't bank speed. Leaving the accumulated fall velocity in
                // place would store energy the ball can never spend and would make
                // the very next frame's floor contact read as a colossal impact.
                // Zero (rather than a bounce) also keeps velocity.Y >= 0, which is
                // what CollisionSystem's landing test requires, so a floor rising
                // into a bottomed-out ball still lands it normally.
                velocity.Y = 0f;
            }
        }
    }
}
