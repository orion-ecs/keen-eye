namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down the Phase C schedules and contracts: the Daily Inferno date-hash
/// seed, medal thresholds, the telegraph-contract tuning constants, the Pulse
/// floor cycle, mode-as-configuration knob shapes, and Flashover Surge trigger
/// determinism.
/// </summary>
public class ModeScheduleTests
{
    #region Daily Inferno date hash

    [Fact]
    public void SeedForDate_FixedDate_YieldsPinnedSeed()
    {
        // yyyyMMdd through one SplitMix64 step. If this value moves, every
        // player's daily shaft changes — a contract, not an implementation detail.
        Assert.Equal(17427370370529247301UL, DailySchedule.SeedForDate(new DateOnly(2026, 7, 24)));
        Assert.Equal(15213845624156854423UL, DailySchedule.SeedForDate(new DateOnly(2024, 1, 1)));
    }

    [Fact]
    public void SeedForDate_SameDate_IsStableAcrossCalls()
    {
        var date = new DateOnly(2026, 7, 24);
        Assert.Equal(DailySchedule.SeedForDate(date), DailySchedule.SeedForDate(date));
    }

    [Fact]
    public void SeedForDate_DifferentDates_YieldDifferentSeeds()
    {
        Assert.NotEqual(
            DailySchedule.SeedForDate(new DateOnly(2026, 7, 24)),
            DailySchedule.SeedForDate(new DateOnly(2026, 7, 25)));
    }

    [Fact]
    public void DateKey_PacksAsYyyyMmDd()
    {
        Assert.Equal(20260724, DailySchedule.DateKey(new DateOnly(2026, 7, 24)));
    }

    [Fact]
    public void MedalForDepth_Thresholds_MapToBronzeSilverGold()
    {
        Assert.Equal(0, DailySchedule.MedalForDepth(149f));
        Assert.Equal(1, DailySchedule.MedalForDepth(150f));
        Assert.Equal(1, DailySchedule.MedalForDepth(274f));
        Assert.Equal(2, DailySchedule.MedalForDepth(275f));
        Assert.Equal(3, DailySchedule.MedalForDepth(375f));
        Assert.Equal(3, DailySchedule.MedalForDepth(9999f));
    }

    #endregion

    #region Telegraph contract (tuning regression guard)

    [Fact]
    public void TelegraphContract_BrittleCrumbleDelay_IsAtLeastPointSixSeconds()
    {
        // THE hard rule from the design: every hazard gives >= 0.6 s of
        // visible/audible warning before it can kill. The Brittle crack visual
        // and crackle SFX start at landing; the crumble comes this much later.
        Assert.True(
            Tuning.BrittleCrumbleDelaySeconds >= 0.6f,
            $"Brittle crumble delay {Tuning.BrittleCrumbleDelaySeconds}s violates the 0.6s telegraph contract");
    }

    [Fact]
    public void TelegraphContract_PulseCloseTelegraph_IsAtLeastPointSixSeconds()
    {
        Assert.True(
            Tuning.PulseCloseTelegraphSeconds >= 0.6f,
            $"Pulse close telegraph {Tuning.PulseCloseTelegraphSeconds}s violates the 0.6s telegraph contract");
    }

    [Fact]
    public void PulseOpenness_CycleShape_OpensTelegraphsAndCloses()
    {
        var openEnd = Tuning.PulsePeriodSeconds
            - Tuning.PulseClosedSeconds - Tuning.PulseCloseTelegraphSeconds;

        // Fully open mid-window, fully closed at the period's end.
        Assert.Equal(1f, FloorLayout.PulseOpenness(openEnd / 2f), tolerance: 0.001f);
        Assert.Equal(0f, FloorLayout.PulseOpenness(Tuning.PulsePeriodSeconds - 0.01f), tolerance: 0.001f);

        // The telegraph: openness shrinks monotonically for the full telegraph
        // window before the close — the shrinking edges ARE the warning.
        var previous = 1f;
        const int samples = 20;
        for (var i = 1; i <= samples; i++)
        {
            var t = openEnd + Tuning.PulseCloseTelegraphSeconds * i / samples;
            var openness = FloorLayout.PulseOpenness(t);
            Assert.True(openness <= previous + 0.0001f, "telegraph must shrink monotonically");
            previous = openness;
        }

        Assert.Equal(0f, previous, tolerance: 0.001f);

        // And the cycle wraps: one full period later the shape repeats.
        Assert.Equal(
            FloorLayout.PulseOpenness(0.3f),
            FloorLayout.PulseOpenness(0.3f + Tuning.PulsePeriodSeconds),
            tolerance: 0.001f);
    }

    #endregion

    #region Modes as configuration

    [Fact]
    public void ModeSettings_EmberGarden_TurnsOffEveryLethalKnob()
    {
        var settings = ModeSettings.For(GameMode.EmberGarden);

        Assert.False(settings.CrusherEnabled);
        Assert.False(settings.SurgeEnabled);
        Assert.False(settings.HeatAffectsScore);
        Assert.False(settings.AdrenalineEnabled);
        Assert.Equal(0f, settings.DurationLimitSeconds);
        Assert.Equal(StemMix.PadOnly, settings.Music);
        // Fixed gentle scroll: no escalation at all.
        Assert.Equal(0f, settings.ScrollSpeedPerMeter);
        Assert.Equal(settings.BaseScrollSpeed, settings.MaxScrollSpeed);
    }

    [Fact]
    public void ModeSettings_DailyInferno_IsTheTimeAttackShape()
    {
        var settings = ModeSettings.For(GameMode.DailyInferno);

        Assert.True(settings.CrusherEnabled);
        Assert.Equal(Tuning.DailyDurationSeconds, settings.DurationLimitSeconds);
        Assert.True(settings.HeatDecaysMidAir);
        Assert.True(settings.FloorSpacing < Tuning.FloorSpacing); // denser shaft
        Assert.Equal(Tuning.DailySmashCostMultiplier, settings.SmashCostMultiplier);
    }

    [Fact]
    public void ModeSettings_Freefall_IsTheFlagshipDefault()
    {
        var settings = ModeSettings.For(GameMode.Freefall);

        Assert.True(settings.CrusherEnabled);
        Assert.True(settings.SurgeEnabled);
        Assert.True(settings.AdrenalineEnabled);
        Assert.Equal(0f, settings.DurationLimitSeconds);
        Assert.Equal(1f, settings.SmashCostMultiplier);
        Assert.Equal(Tuning.FloorSpacing, settings.FloorSpacing);
    }

    #endregion

    #region Flashover Surge trigger determinism

    [Fact]
    public void Surge_TriggersExactlyWhenFloorFortyClears_AndClosesTenMusicSecondsLater()
    {
        using var world = CreateSurgeWorld(GameMode.Freefall);

        // Clearing up to floor 39: nothing.
        world.GetSingleton<SurgeState>().DeepestClearedFloor = Tuning.SurgePeriodFloors - 1;
        world.Update(1f / 60f);
        Assert.False(world.GetSingleton<SurgeState>().Active);

        // Clearing floor 40: the window opens, and the next trigger arms at 80.
        world.GetSingleton<SurgeState>().DeepestClearedFloor = Tuning.SurgePeriodFloors;
        world.Update(1f / 60f);

        var surge = world.GetSingleton<SurgeState>();
        Assert.True(surge.Active);
        Assert.Equal(2 * Tuning.SurgePeriodFloors, surge.NextSurgeFloor);
        Assert.Equal(1, world.GetSingleton<RunEventCounters>().SurgeWindows);

        // The window closes after exactly SurgeDurationSeconds of MUSIC time.
        var framesToEnd = (int)(Tuning.SurgeDurationSeconds * 60f) + 2;
        for (var i = 0; i < framesToEnd; i++)
        {
            world.Update(1f / 60f);
        }

        Assert.False(world.GetSingleton<SurgeState>().Active);
        Assert.Equal(1, world.GetSingleton<RunEventCounters>().SurgeWindows);
    }

    [Fact]
    public void Surge_FiveSmashesInsideOneWindow_AwardSweepExactlyOnce()
    {
        using var world = CreateSurgeWorld(GameMode.Freefall);

        world.GetSingleton<SurgeState>().DeepestClearedFloor = Tuning.SurgePeriodFloors;
        world.Update(1f / 60f);
        Assert.True(world.GetSingleton<SurgeState>().Active);

        var awards = 0;
        for (var smash = 0; smash < Tuning.SurgeSweepSmashes + 2; smash++)
        {
            world.GetSingleton<FrameEvents>() = default;
            world.GetSingleton<FrameEvents>().Smashed = true;
            world.Update(1f / 60f);
            if (world.GetSingleton<FrameEvents>().SurgeSweepAwarded)
            {
                awards++;
            }
        }

        Assert.Equal(1, awards);
    }

    [Fact]
    public void Surge_InEmberGarden_NeverTriggers()
    {
        using var world = CreateSurgeWorld(GameMode.EmberGarden);

        world.GetSingleton<SurgeState>().DeepestClearedFloor = 10 * Tuning.SurgePeriodFloors;
        world.Update(1f / 60f);

        Assert.False(world.GetSingleton<SurgeState>().Active);
    }

    #endregion

    /// <summary>
    /// Builds a minimal world containing only the music clock and surge
    /// systems plus the singletons they read.
    /// </summary>
    private static World CreateSurgeWorld(GameMode mode)
    {
        var world = new World();
        world.SetSingleton(new GameState { Phase = GamePhase.Playing });
        world.SetSingleton(new TimeScale { Value = 1f });
        world.SetSingleton(new RunConfig { Mode = mode, Settings = ModeSettings.For(mode) });
        world.SetSingleton(new MusicClock());
        world.SetSingleton(new SurgeState { NextSurgeFloor = Tuning.SurgePeriodFloors });
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new RunEventCounters());
        world.AddSystem<MusicClockSystem>(SystemPhase.Update, order: 0);
        world.AddSystem<SurgeSystem>(SystemPhase.Update, order: 10);
        return world;
    }
}
