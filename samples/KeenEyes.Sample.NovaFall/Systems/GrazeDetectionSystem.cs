using System.Numerics;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Detects Graze Sparks: the ball threading a gap within
/// <see cref="Tuning.GrazeDistance"/> design units of a floor edge without
/// touching it. Near-miss as skill expression — grazes award score, a little
/// heat, and a rising-pitch ting per consecutive graze.
/// </summary>
/// <remarks>
/// <para>
/// TEACHING NOTE — this system is the deliberate counterpoint to
/// <see cref="CollisionSystem"/>'s flat row scan. Collision tests one ball
/// against ~15 floors, so a scan wins there. Graze detection, however, is a
/// <em>proximity</em> question ("what is near the ball right now?"), and the
/// shaft is about to get busier: Floor Smash showers it with fragment bursts,
/// and later phases add more floor types. A spatial index answers "near X"
/// in O(log n) no matter how crowded the neighborhood gets, so this is where
/// the quadtree (<c>KeenEyes.Spatial</c>, installed with quadtree strategy)
/// earns its keep. The index is refreshed from floor movement each LateUpdate,
/// so a query here sees positions at most one frame old — well within the
/// graze tolerance, and identically so on every run, which keeps the check
/// deterministic.
/// </para>
/// <para>
/// The system runs BEFORE <see cref="CollisionSystem"/> and keys off
/// <see cref="Floor.Cleared"/>: a floor whose plane the ball's center has just
/// passed, but that collision has not yet marked cleared, is being crossed
/// this exact frame — the only moment a graze can happen.
/// </para>
/// </remarks>
public sealed class GrazeDetectionSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        // The quadtree is installed by the SpatialPlugin in BOTH the windowed
        // and headless bootstrap paths — graze is gameplay, not juice, and must
        // behave identically in each.
        if (!World.TryGetExtension<SpatialQueryApi>(out var spatial) || spatial is null)
        {
            return;
        }

        foreach (var ballEntity in World.Query<Ball, Position2D, Velocity2D>())
        {
            if (World.Has<RestingOn>(ballEntity))
            {
                continue;
            }

            var radius = World.Get<Ball>(ballEntity).Radius;
            ref readonly var position = ref World.Get<Position2D>(ballEntity);

            foreach (var floorEntity in spatial.QueryRadius(
                new Vector3(position.X, position.Y, 0f), Tuning.GrazeQueryRadius))
            {
                // The index can briefly contain entities despawned this frame;
                // validate before touching components.
                if (!World.IsAlive(floorEntity) || !World.Has<Floor>(floorEntity))
                {
                    continue;
                }

                ref readonly var floor = ref World.Get<Floor>(floorEntity);
                var floorTop = World.Get<Position2D>(floorEntity).Y;

                // Crossing right now: center below the slab, not yet marked
                // cleared by the collision system (which runs after us).
                if (floor.Cleared || position.Y <= floorTop + floor.Thickness)
                {
                    continue;
                }

                var gapLeft = floor.GapCenterX - floor.GapWidth / 2f;
                var gapRight = floor.GapCenterX + floor.GapWidth / 2f;
                var clearance = Math.Min(
                    (position.X - radius) - gapLeft,
                    gapRight - (position.X + radius));

                // Fully inside the gap (it threaded, not clipped), but within
                // graze distance of the nearer edge.
                if (clearance >= 0f && clearance <= Tuning.GrazeDistance)
                {
                    ref var events = ref World.GetSingleton<FrameEvents>();
                    events.Grazes++;
                    events.GrazeX = position.X;
                    events.GrazeY = floorTop + floor.Thickness / 2f;

                    World.GetSingleton<RunEventCounters>().Grazes++;
                }
            }
        }
    }
}
