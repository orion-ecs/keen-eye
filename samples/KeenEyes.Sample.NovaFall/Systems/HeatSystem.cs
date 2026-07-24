namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Manages the Heat resource: clean gap-throughs stoke it, landings halve it,
/// resting bleeds it slowly, and thresholds map it to the four tiers that gate
/// the score multiplier.
/// </summary>
/// <remarks>
/// Tier progression: Ember (x1) → Flame (x2) → Plasma (x4) → Nova (x8).
/// A full-stop landing HALVES heat but never zeroes it; only death resets heat.
/// </remarks>
public sealed class HeatSystem : SystemBase
{
    /// <summary>
    /// Gets the score multiplier for a heat tier (1, 2, 4, or 8).
    /// </summary>
    /// <param name="tier">The tier index from <see cref="HeatState.Tier"/>.</param>
    /// <returns>The score multiplier for the tier.</returns>
    public static int MultiplierForTier(int tier) => 1 << tier;

    /// <summary>
    /// Gets the display name of a heat tier.
    /// </summary>
    /// <param name="tier">The tier index from <see cref="HeatState.Tier"/>.</param>
    /// <returns>The tier's display name.</returns>
    public static string NameForTier(int tier) => tier switch
    {
        3 => "NOVA",
        2 => "PLASMA",
        1 => "FLAME",
        _ => "EMBER",
    };

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        var dt = deltaTime * World.GetSingleton<TimeScale>().Value;
        ref var heat = ref World.GetSingleton<HeatState>();
        ref var events = ref World.GetSingleton<FrameEvents>();

        // Stoke: every clean gap-through adds heat.
        if (events.GapsPassed > 0)
        {
            heat.Heat = Math.Min(heat.Heat + events.GapsPassed * Tuning.HeatPerGap, Tuning.MaxHeat);
        }

        // A full-stop landing halves heat — punishing, but never back to zero.
        if (events.Landed)
        {
            heat.Heat *= 0.5f;
        }

        // Resting on a floor slowly bleeds heat away.
        var resting = false;
        foreach (var _ in World.Query<Ball, RestingOn>())
        {
            resting = true;
            break;
        }

        if (resting)
        {
            heat.Heat = Math.Max(heat.Heat - Tuning.HeatDecayPerSecond * dt, 0f);
        }

        heat.Tier = TierForHeat(heat.Heat);

        // Events are consumed once per frame.
        events = default;
    }

    private static int TierForHeat(float heat) => heat switch
    {
        >= Tuning.NovaThreshold => 3,
        >= Tuning.PlasmaThreshold => 2,
        >= Tuning.FlameThreshold => 1,
        _ => 0,
    };
}
