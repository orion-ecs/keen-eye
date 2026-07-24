using System.Numerics;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Samples the ball's position into the <see cref="TrailState"/> ring buffer
/// each frame. The render system turns the buffer into the comet-trail ribbon
/// (length and width scaled by heat tier, alpha fading toward the tail).
/// </summary>
/// <remarks>
/// A ring buffer is the natural shape for "the last N positions": one write and
/// two index updates per frame, no shifting, no allocation after the first
/// frame. The buffer is presentation state only — nothing in the simulation
/// reads it — so the trail can never perturb determinism.
/// </remarks>
public sealed class TrailSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        ref var trail = ref World.GetSingleton<TrailState>();

        if (!juice.PresentationAvailable)
        {
            return;
        }

        if (!juice.Enabled || World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            // Juice off or not playing: let the trail vanish instantly.
            trail.Count = 0;
            return;
        }

        trail.Points ??= new Vector2[Tuning.TrailCapacity];

        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ref readonly var position = ref World.Get<Position2D>(entity);

            trail.Head = (trail.Head + 1) % trail.Points.Length;
            trail.Points[trail.Head] = new Vector2(position.X, position.Y);
            trail.Count = Math.Min(trail.Count + 1, trail.Points.Length);
            break;
        }
    }
}
