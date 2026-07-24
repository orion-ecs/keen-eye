namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down the heat-tier ladder: gap-throughs stoke heat across the exact
/// tier thresholds, the multiplier ladder follows, landings halve, and the
/// Floor Smash charges one tier span (scaled by the mode's cost multiplier).
/// </summary>
public class HeatTierTests
{
    #region Tier ladder

    [Fact]
    public void HeatSystem_ThreadingGaps_ClimbsTheExactTierLadder()
    {
        using var world = CreateHeatWorld(GameMode.Freefall);

        // 12 heat per gap: the ladder crosses Flame at 30, Plasma at 70,
        // Nova at 120. Expected tier after each of ten gap-throughs:
        int[] expectedTiers = [0, 0, 1, 1, 1, 2, 2, 2, 2, 3];
        int[] expectedMultipliers = [1, 1, 2, 2, 2, 4, 4, 4, 4, 8];

        for (var gap = 0; gap < expectedTiers.Length; gap++)
        {
            PumpFrame(world, events => events.GapsPassed = 1);

            var heat = world.GetSingleton<HeatState>();
            Assert.Equal((gap + 1) * Tuning.HeatPerGap, heat.Heat, tolerance: 0.001f);
            Assert.Equal(expectedTiers[gap], heat.Tier);
            Assert.Equal(expectedMultipliers[gap], HeatSystem.MultiplierForTier(heat.Tier));
        }
    }

    [Fact]
    public void HeatSystem_Landing_HalvesHeatButNeverZeroes()
    {
        using var world = CreateHeatWorld(GameMode.Freefall);

        // Stoke to Nova (10 gaps = 120 heat), then land.
        for (var gap = 0; gap < 10; gap++)
        {
            PumpFrame(world, events => events.GapsPassed = 1);
        }

        PumpFrame(world, events => events.Landed = true);

        var heat = world.GetSingleton<HeatState>();
        Assert.Equal(60f, heat.Heat, tolerance: 0.001f);
        Assert.Equal(1, heat.Tier); // 60 is Flame: above 30, below 70.
        Assert.True(heat.Heat > 0f, "a landing must never zero heat");
    }

    [Fact]
    public void HeatSystem_BumperLaunch_HalvesHeatLikeALanding()
    {
        using var world = CreateHeatWorld(GameMode.Freefall);

        for (var gap = 0; gap < 6; gap++)
        {
            PumpFrame(world, events => events.GapsPassed = 1);
        }

        PumpFrame(world, events => events.Bumped = true);

        Assert.Equal(36f, world.GetSingleton<HeatState>().Heat, tolerance: 0.001f);
    }

    #endregion

    #region Smash cost

    [Fact]
    public void HeatSystem_Smash_ChargesOneFullTierSpan()
    {
        using var world = CreateHeatWorld(GameMode.Freefall);

        // 6 gaps = 72 heat = Plasma. A smash at Plasma costs the span between
        // the Plasma and Flame thresholds: 70 - 30 = 40.
        for (var gap = 0; gap < 6; gap++)
        {
            PumpFrame(world, events => events.GapsPassed = 1);
        }

        PumpFrame(world, events => events.Smashed = true);

        var heat = world.GetSingleton<HeatState>();
        Assert.Equal(32f, heat.Heat, tolerance: 0.001f);
        Assert.Equal(1, heat.Tier);
    }

    [Fact]
    public void HeatSystem_SmashInDailyInferno_CostsHalf()
    {
        using var world = CreateHeatWorld(GameMode.DailyInferno);

        for (var gap = 0; gap < 6; gap++)
        {
            PumpFrame(world, events => events.GapsPassed = 1);
        }

        PumpFrame(world, events => events.Smashed = true);

        // Half of the 40-point Plasma span (72 - 20 = 52), minus the seven
        // frames of Daily Inferno's mid-air bleed that ticked while pumping.
        var expected = 52f - 7f * Tuning.DailyMidAirHeatDecayPerSecond / 60f;
        Assert.Equal(expected, world.GetSingleton<HeatState>().Heat, tolerance: 0.01f);
    }

    #endregion

    #region Mode decay knobs

    [Fact]
    public void HeatSystem_DailyInferno_BleedsHeatMidAir()
    {
        using var world = CreateHeatWorld(GameMode.DailyInferno);

        PumpFrame(world, events => events.GapsPassed = 1);
        var stoked = world.GetSingleton<HeatState>().Heat;

        // One idle airborne second: Daily Inferno decays, Freefall would not.
        for (var frame = 0; frame < 60; frame++)
        {
            PumpFrame(world, _ => { });
        }

        var expected = stoked - Tuning.DailyMidAirHeatDecayPerSecond;
        Assert.Equal(expected, world.GetSingleton<HeatState>().Heat, tolerance: 0.1f);
    }

    [Fact]
    public void HeatSystem_Freefall_KeepsHeatMidAir()
    {
        using var world = CreateHeatWorld(GameMode.Freefall);

        PumpFrame(world, events => events.GapsPassed = 1);
        var stoked = world.GetSingleton<HeatState>().Heat;

        for (var frame = 0; frame < 60; frame++)
        {
            PumpFrame(world, _ => { });
        }

        Assert.Equal(stoked, world.GetSingleton<HeatState>().Heat, tolerance: 0.001f);
    }

    #endregion

    /// <summary>
    /// Builds a minimal world containing only <see cref="HeatSystem"/> and the
    /// singletons it reads — the manager-style isolation test: no ball, no
    /// floors, events injected by hand.
    /// </summary>
    private static World CreateHeatWorld(GameMode mode)
    {
        var world = new World();
        world.SetSingleton(new GameState { Phase = GamePhase.Playing });
        world.SetSingleton(new TimeScale { Value = 1f });
        world.SetSingleton(new RunConfig { Mode = mode, Settings = ModeSettings.For(mode) });
        world.SetSingleton(new HeatState());
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new ComboState());
        world.AddSystem<HeatSystem>(SystemPhase.Update, order: 0);
        return world;
    }

    /// <summary>
    /// Injects one frame's events and steps the world a fixed 1/60 s.
    /// </summary>
    private static void PumpFrame(World world, Action<FrameEventsWriter> writeEvents)
    {
        var writer = new FrameEventsWriter(world);
        writeEvents(writer);
        world.Update(1f / 60f);
        world.GetSingleton<FrameEvents>() = default;
    }

    /// <summary>
    /// Tiny helper so tests can write event fields through a lambda without
    /// wrestling with ref-struct capture rules.
    /// </summary>
    private sealed class FrameEventsWriter(World world)
    {
        /// <summary>Sets the number of clean gap-throughs this frame.</summary>
        public int GapsPassed
        {
            set => world.GetSingleton<FrameEvents>().GapsPassed = value;
        }

        /// <summary>Sets whether the ball landed this frame.</summary>
        public bool Landed
        {
            set => world.GetSingleton<FrameEvents>().Landed = value;
        }

        /// <summary>Sets whether the ball smashed a floor this frame.</summary>
        public bool Smashed
        {
            set => world.GetSingleton<FrameEvents>().Smashed = value;
        }

        /// <summary>Sets whether a Bumper launched the ball this frame.</summary>
        public bool Bumped
        {
            set => world.GetSingleton<FrameEvents>().Bumped = value;
        }
    }
}
