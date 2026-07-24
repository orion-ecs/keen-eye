namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Choreographs the death beat: white full-screen flash, slow-motion ball
/// shatter, 400 ms of true silence, then the score card.
/// </summary>
/// <remarks>
/// <para>
/// Timeline (real seconds after the crush):
/// <list type="bullet">
///   <item><description>0.0 — flash at full white, time snaps to 20%, the ember
///   shatter burst is requested (spawned by <see cref="VfxSystem"/>).</description></item>
///   <item><description>0.0-0.35 — flash fades.</description></item>
///   <item><description>0.4 — ALL audio stops (<see cref="NovaFallAudioSystem"/>
///   reads <see cref="DeathSequenceState.AudioSilenced"/>).</description></item>
///   <item><description>0.5 — normal time resumes.</description></item>
///   <item><description>0.8 — the score card appears.</description></item>
/// </list>
/// </para>
/// <para>
/// The timer runs on REAL delta time: the sequence slows the world down, so it
/// cannot clock itself off the world's own slowed time. It only runs when
/// presentation is available — headless death is just a phase change, exactly
/// as in Phase A.
/// </para>
/// </remarks>
public sealed class DeathSequenceSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        if (!juice.PresentationAvailable)
        {
            return;
        }

        ref var death = ref World.GetSingleton<DeathSequenceState>();
        var phase = World.GetSingleton<GameState>().Phase;

        if (phase != GamePhase.Dead)
        {
            // Restart happened (StartRun resets DeathSequenceState too, but a
            // system should not depend on who resets shared state first).
            death = default;
            return;
        }

        ref var timeScale = ref World.GetSingleton<TimeScale>();

        if (!death.Active)
        {
            death.Active = true;
            death.Timer = 0f;
            death.FlashAlpha = juice.Enabled ? 1f : 0f;
            timeScale.Value = juice.Enabled ? Tuning.DeathSlowMo : 1f;
            return;
        }

        death.Timer += deltaTime;
        death.FlashAlpha = Math.Max(0f, 1f - death.Timer / Tuning.DeathFlashSeconds);

        if (death.Timer >= Tuning.DeathSilenceStart)
        {
            death.AudioSilenced = true;
        }

        if (death.Timer >= Tuning.DeathSlowMoSeconds)
        {
            timeScale.Value = 1f;
        }

        if (death.Timer >= Tuning.DeathSilenceStart + Tuning.DeathSilenceSeconds)
        {
            death.ScoreCardVisible = true;
        }
    }
}
