namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Owns the Floor Smash hitstop: freezes simulation time for a few frames after
/// a smash by zeroing the <see cref="TimeScale"/> singleton, then restores it.
/// </summary>
/// <remarks>
/// <para>
/// Runs in EarlyUpdate, so it sees the previous frame's <see cref="FrameEvents"/>
/// (the impact frame renders at full speed, then the freeze lands — the classic
/// fighting-game cadence). The countdown is in real frames, never scaled time:
/// a clock stopped by the hitstop could never restart itself.
/// </para>
/// <para>
/// This is simulation, not juice — the freeze is part of Floor Smash's feel AND
/// its balance (a beat of aim time after every smash), so it runs identically in
/// the headless <c>--simulate</c> mode and is fully deterministic.
/// </para>
/// </remarks>
public sealed class HitstopSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var state = ref World.GetSingleton<HitstopState>();
        ref var timeScale = ref World.GetSingleton<TimeScale>();

        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            // Death choreography owns TimeScale outside of play.
            state.FramesRemaining = 0;
            return;
        }

        if (World.GetSingleton<FrameEvents>().Smashed)
        {
            state.FramesRemaining = Tuning.SmashHitstopFrames;
        }

        if (state.FramesRemaining > 0)
        {
            state.FramesRemaining--;
            timeScale.Value = 0f;
        }
        else
        {
            timeScale.Value = 1f;
        }
    }
}
