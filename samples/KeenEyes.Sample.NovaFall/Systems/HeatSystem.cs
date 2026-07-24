namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Manages the Heat resource: clean gap-throughs stoke it, grazes add a little,
/// landings halve it, resting bleeds it slowly, a Floor Smash charges one full
/// tier, and thresholds map it to the four tiers that gate the score multiplier.
/// Also keeps the combo and graze-chain counters in step with those events.
/// </summary>
/// <remarks>
/// <para>
/// Tier progression: Ember (x1) → Flame (x2) → Plasma (x4) → Nova (x8).
/// A full-stop landing HALVES heat but never zeroes it; only death resets heat.
/// </para>
/// <para>
/// When the tier changes, the transition is published into
/// <see cref="FrameEvents"/> so the palette, camera, and audio systems can react.
/// Events are consumed here but NOT cleared — <see cref="FrameEventsClearSystem"/>
/// zeroes them at the start of the next frame, after the juice systems (and next
/// frame's camera/hitstop) have seen them.
/// </para>
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

    /// <summary>
    /// Gets the heat value at which a tier begins.
    /// </summary>
    /// <param name="tier">The tier index from <see cref="HeatState.Tier"/>.</param>
    /// <returns>The tier's entry threshold (0 for Ember).</returns>
    public static float ThresholdForTier(int tier) => tier switch
    {
        3 => Tuning.NovaThreshold,
        2 => Tuning.PlasmaThreshold,
        1 => Tuning.FlameThreshold,
        _ => 0f,
    };

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        var dt = deltaTime * World.GetSingleton<TimeScale>().Value;
        var settings = World.GetSingleton<RunConfig>().Settings;
        ref var heat = ref World.GetSingleton<HeatState>();
        ref var events = ref World.GetSingleton<FrameEvents>();
        ref var combo = ref World.GetSingleton<ComboState>();

        var previousTier = heat.Tier;

        // Stoke: every clean gap-through adds heat and extends the combo.
        if (events.GapsPassed > 0)
        {
            heat.Heat = Math.Min(heat.Heat + events.GapsPassed * Tuning.HeatPerGap, Tuning.MaxHeat);
            combo.Combo += events.GapsPassed;
            combo.MaxCombo = Math.Max(combo.MaxCombo, combo.Combo);
        }

        // Grazes add a smaller bonus and extend the consecutive-graze chain
        // that drives the rising ting pitch.
        if (events.Grazes > 0)
        {
            heat.Heat = Math.Min(heat.Heat + events.Grazes * Tuning.GrazeHeatBonus, Tuning.MaxHeat);
            combo.ConsecutiveGrazes += events.Grazes;
        }

        // A Floor Smash spends one full tier: exactly the heat span between the
        // current tier's entry threshold and the one below it. Daily Inferno
        // halves the cost via its mode settings.
        if (events.Smashed)
        {
            var cost = (ThresholdForTier(previousTier) - ThresholdForTier(previousTier - 1))
                * settings.SmashCostMultiplier;
            heat.Heat = Math.Max(heat.Heat - cost, 0f);
        }

        // Any full-stop floor touch — a landing or a Bumper launch — halves
        // heat (punishing, but never back to zero) and breaks both the combo
        // and the graze chain.
        if (events.Landed || events.Bumped)
        {
            heat.Heat *= 0.5f;
            combo.Combo = 0;
            combo.ConsecutiveGrazes = 0;
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
        else if (settings.HeatDecaysMidAir)
        {
            // Daily Inferno's pressure: even the air is cold today.
            heat.Heat = Math.Max(heat.Heat - Tuning.DailyMidAirHeatDecayPerSecond * dt, 0f);
        }

        heat.Tier = TierForHeat(heat.Heat);

        if (heat.Tier != previousTier)
        {
            events.TierChanged = true;
            events.TierFrom = previousTier;
            events.TierTo = heat.Tier;
        }
    }

    private static int TierForHeat(float heat) => heat switch
    {
        >= Tuning.NovaThreshold => 3,
        >= Tuning.PlasmaThreshold => 2,
        >= Tuning.FlameThreshold => 1,
        _ => 0,
    };
}
