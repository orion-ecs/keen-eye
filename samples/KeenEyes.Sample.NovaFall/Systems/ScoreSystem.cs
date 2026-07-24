namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Accumulates score continuously — meters fallen multiplied by the current heat
/// tier's multiplier — plus flat bonuses for grazes and Floor Smashes.
/// </summary>
/// <remarks>
/// Score integrates per frame from the depth delta, so the multiplier active
/// <em>while</em> each meter is fallen is what counts — stoking heat early
/// compounds for the whole run. Bonuses are read from the same
/// <see cref="FrameEvents"/> the heat system consumes; because events are only
/// cleared at the start of the next frame, every consumer sees them exactly once.
/// </remarks>
public sealed class ScoreSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        ref var score = ref World.GetSingleton<ScoreState>();
        var depth = World.GetSingleton<ScrollState>().Depth;
        var tier = World.GetSingleton<HeatState>().Tier;
        ref readonly var events = ref World.GetSingleton<FrameEvents>();

        var metersThisFrame = depth - score.LastDepth;
        if (metersThisFrame > 0f)
        {
            score.Score += metersThisFrame * HeatSystem.MultiplierForTier(tier);
        }

        score.LastDepth = depth;

        // Flat event bonuses: near-misses and Floor Smashes.
        score.Score += events.Grazes * Tuning.GrazeScoreBonus;
        if (events.Smashed)
        {
            score.Score += Tuning.SmashScoreBonus;
        }
    }
}
