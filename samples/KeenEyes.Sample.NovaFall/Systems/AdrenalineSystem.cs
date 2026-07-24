namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Runs the Adrenaline Save window: after <see cref="CrushSystem"/> spends the
/// run's one save, this system holds simulation time at 20%, ticks the window
/// down on RAW delta time, and resolves it — survival if the ball steers clear
/// of the crush zone, or time restored (and the next Furnace touch fatal) if
/// the window expires.
/// </summary>
/// <remarks>
/// <para>
/// The window is <see cref="Tuning.AdrenalineRealSeconds"/> of REAL time by
/// design — the whole point is 1.5 wall-perceived seconds of slow motion — so
/// it accumulates raw (unscaled) delta time: a timer multiplied by the very
/// <see cref="TimeScale"/> it sets would run five times too long. Raw delta
/// time is still fully deterministic in <c>--simulate</c>, where the timestep
/// is fixed.
/// </para>
/// <para>
/// Runs in EarlyUpdate AFTER <see cref="HitstopSystem"/> (which restores
/// <see cref="TimeScale"/> to 1 whenever no hitstop is pending), so the save's
/// slow motion wins the frame while still letting a smash hitstop freeze
/// harder than it.
/// </para>
/// </remarks>
public sealed class AdrenalineSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var adrenaline = ref World.GetSingleton<AdrenalineState>();

        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing || !adrenaline.Active)
        {
            return;
        }

        ref var timeScale = ref World.GetSingleton<TimeScale>();

        // Escape check: fully below the crush zone means the save worked.
        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            if (position.Y - ball.Radius > Tuning.CeilingY + Tuning.AdrenalineEscapeMargin)
            {
                adrenaline.Active = false;
                timeScale.Value = 1f;
                World.GetSingleton<FrameEvents>().AdrenalineSurvived = true;
                return;
            }

            break;
        }

        adrenaline.RealSecondsRemaining -= deltaTime;
        if (adrenaline.RealSecondsRemaining <= 0f)
        {
            // Window over, still in the zone: time resumes, and if the ball is
            // still touching the Furnace, CrushSystem finishes the job this
            // frame — the save is spent, so there is no second chance.
            adrenaline.Active = false;
            timeScale.Value = 1f;
            return;
        }

        // Hold the slow motion — unless a smash hitstop froze time entirely,
        // which reads better than fighting it.
        if (World.GetSingleton<HitstopState>().FramesRemaining == 0)
        {
            timeScale.Value = Tuning.AdrenalineTimeScale;
        }
    }
}
