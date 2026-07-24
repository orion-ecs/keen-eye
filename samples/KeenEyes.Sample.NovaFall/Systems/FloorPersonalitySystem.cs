namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Advances per-floor personality timers: Brittle floors crack toward their
/// crumble, and Bumper wobbles ring down.
/// </summary>
/// <remarks>
/// <para>
/// TELEGRAPH CONTRACT — a cracking Brittle floor crumbles exactly
/// <see cref="Tuning.BrittleCrumbleDelaySeconds"/> scaled seconds after the
/// landing that cracked it. The crack visual (drawn by the render system from
/// <see cref="Floor.CrackSeconds"/>) and the crackle SFX both start at the
/// landing instant, so the hazard gives its full &gt;= 0.6 s of warning before
/// the floor — and whatever was standing on it — drops.
/// </para>
/// <para>
/// Runs after <see cref="FloorScrollSystem"/> and before
/// <see cref="CollisionSystem"/>: a floor that crumbles this frame is gone
/// before collision runs, so the ball simply finds nothing under it and falls —
/// no special "the ground vanished" handling anywhere else. (The stale
/// <c>RestingOn</c> reference is cleaned up by collision's existing dead-floor
/// check.)
/// </para>
/// </remarks>
public sealed class FloorPersonalitySystem : SystemBase
{
    private readonly List<Entity> crumbleList = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        var dt = deltaTime * World.GetSingleton<TimeScale>().Value;
        ref var events = ref World.GetSingleton<FrameEvents>();

        // Collect crumbles first, despawn after: never structurally modify the
        // world during query iteration.
        crumbleList.Clear();

        foreach (var entity in World.Query<Floor, Position2D>())
        {
            ref var floor = ref World.Get<Floor>(entity);

            if (floor.WobbleSeconds > 0f)
            {
                floor.WobbleSeconds = Math.Max(floor.WobbleSeconds - dt, 0f);
            }

            if (!floor.Cracking)
            {
                continue;
            }

            floor.CrackSeconds += dt;
            if (floor.CrackSeconds >= Tuning.BrittleCrumbleDelaySeconds)
            {
                events.Crumbled = true;
                events.CrumbleY = World.Get<Position2D>(entity).Y;
                events.CrumbleGapCenterX = floor.GapCenterX;
                events.CrumbleGapWidth = floor.GapWidth;
                crumbleList.Add(entity);
            }
        }

        if (crumbleList.Count == 0)
        {
            return;
        }

        World.GetSingleton<RunEventCounters>().Crumbles += crumbleList.Count;
        foreach (var entity in crumbleList)
        {
            World.Despawn(entity);
        }
    }
}
