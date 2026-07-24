namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Runs the Flashover Surge: every <see cref="Tuning.SurgePeriodFloors"/>
/// cleared floors, a <see cref="Tuning.SurgeDurationSeconds"/>-second window in
/// which the scroll spikes, EVERY floor is smashable at any heat tier, the
/// shaft burns white-hot, and the lead stem swaps to its surge variant.
/// Five or more Floor Smashes inside one window earn the Surge Sweep bonus.
/// </summary>
/// <remarks>
/// <para>
/// DETERMINISM — the trigger is a FLOOR INDEX (fed by <see cref="CollisionSystem"/>
/// as the ball clears floors), and the window is timed on the
/// <see cref="MusicClock"/>. Neither wall-clock time nor frame rate appears
/// anywhere, so the headless <c>--simulate</c> mode enters and leaves every
/// surge on exactly the same frames, every run.
/// </para>
/// <para>
/// This system owns the <see cref="SurgeState"/> bookkeeping and the sweep
/// bonus event; the systems it energizes stay ignorant of WHY — collision reads
/// <see cref="SurgeState.Active"/> for smashability, the scroll system for the
/// speed spike, the palette/audio systems for the white-hot tint and stem swap.
/// </para>
/// </remarks>
public sealed class SurgeSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        ref var surge = ref World.GetSingleton<SurgeState>();
        if (!World.GetSingleton<RunConfig>().Settings.SurgeEnabled)
        {
            return;
        }

        var clock = World.GetSingleton<MusicClock>().Seconds;
        ref var events = ref World.GetSingleton<FrameEvents>();

        if (!surge.Active)
        {
            if (surge.DeepestClearedFloor >= surge.NextSurgeFloor)
            {
                surge.Active = true;
                surge.EndsAtSeconds = clock + Tuning.SurgeDurationSeconds;
                surge.SmashesThisSurge = 0;
                surge.SweepAwarded = false;
                surge.NextSurgeFloor += Tuning.SurgePeriodFloors;
                World.GetSingleton<RunEventCounters>().SurgeWindows++;
                events.SurgeStarted = true;
            }

            return;
        }

        // Count this window's smashes toward the Surge Sweep bonus.
        if (events.Smashed)
        {
            surge.SmashesThisSurge++;
            if (!surge.SweepAwarded && surge.SmashesThisSurge >= Tuning.SurgeSweepSmashes)
            {
                surge.SweepAwarded = true;
                events.SurgeSweepAwarded = true;
            }
        }

        if (clock >= surge.EndsAtSeconds)
        {
            surge.Active = false;
            events.SurgeEnded = true;
        }
    }
}
