namespace KeenEyes.Sample.NovaFall;

// ============================================================================
// Modes as configuration. All three modes run the exact same systems; a mode
// is nothing but a bundle of knob values captured into RunConfig at run start.
// This is the "determinism as configuration" idea from Phase A taken one step
// further: if a mode needed its own code path, it would be a different game.
// ============================================================================

/// <summary>
/// The selectable game modes.
/// </summary>
public enum GameMode
{
    /// <summary>The flagship endless mode: escalating scroll, the Furnace, surges.</summary>
    Freefall,

    /// <summary>Three-minute time attack on a date-hashed seed, three attempts per day.</summary>
    DailyInferno,

    /// <summary>Zen mode: no crusher, fixed gentle scroll, heat drives visuals only.</summary>
    EmberGarden,
}

/// <summary>
/// Which music stems a mode mixes (see <see cref="NovaFallAudioSystem"/>).
/// </summary>
public enum StemMix
{
    /// <summary>Pad always, pulse from Flame, lead from Plasma, surge lead during Flashover.</summary>
    Full,

    /// <summary>Only the pad stem, ducked — Ember Garden's ambient bed.</summary>
    PadOnly,
}

/// <summary>
/// The complete set of knobs that make a mode a mode. Captured into
/// <see cref="RunConfig.Settings"/> when a run starts; every mode-sensitive
/// system reads these instead of hard-coded <see cref="Tuning"/> constants.
/// </summary>
public struct ModeSettings
{
    /// <summary>When false, the Furnace ceiling clamps the ball instead of killing it.</summary>
    public bool CrusherEnabled;

    /// <summary>Run length in scaled-simulation seconds; 0 means endless.</summary>
    public float DurationLimitSeconds;

    /// <summary>When true, heat also decays while airborne (Daily Inferno's pressure).</summary>
    public bool HeatDecaysMidAir;

    /// <summary>Vertical distance between consecutive floors.</summary>
    public float FloorSpacing;

    /// <summary>Multiplier on the Floor Smash heat cost (0.5 in Daily Inferno).</summary>
    public float SmashCostMultiplier;

    /// <summary>Scroll speed at depth zero.</summary>
    public float BaseScrollSpeed;

    /// <summary>Additional scroll speed per meter of depth (0 = fixed speed).</summary>
    public float ScrollSpeedPerMeter;

    /// <summary>Upper bound on scroll speed.</summary>
    public float MaxScrollSpeed;

    /// <summary>When false, Flashover Surges never trigger.</summary>
    public bool SurgeEnabled;

    /// <summary>When false, the score multiplier is fixed at x1 — heat drives visuals only.</summary>
    public bool HeatAffectsScore;

    /// <summary>When false, the once-per-run Adrenaline Save is unavailable.</summary>
    public bool AdrenalineEnabled;

    /// <summary>The mode's music stem mix.</summary>
    public StemMix Music;

    /// <summary>
    /// Builds the settings bundle for a mode. A pure function: the same mode
    /// always yields the same knobs, so a (mode, seed) pair fully determines a run.
    /// </summary>
    /// <param name="mode">The mode to configure.</param>
    /// <returns>The mode's knob values.</returns>
    public static ModeSettings For(GameMode mode) => mode switch
    {
        GameMode.DailyInferno => new ModeSettings
        {
            CrusherEnabled = true,
            DurationLimitSeconds = Tuning.DailyDurationSeconds,
            HeatDecaysMidAir = true,
            FloorSpacing = Tuning.DailyFloorSpacing,
            SmashCostMultiplier = Tuning.DailySmashCostMultiplier,
            BaseScrollSpeed = Tuning.BaseScrollSpeed,
            ScrollSpeedPerMeter = Tuning.ScrollSpeedPerMeter,
            MaxScrollSpeed = Tuning.MaxScrollSpeed,
            SurgeEnabled = true,
            HeatAffectsScore = true,
            AdrenalineEnabled = true,
            Music = StemMix.Full,
        },
        GameMode.EmberGarden => new ModeSettings
        {
            CrusherEnabled = false,
            DurationLimitSeconds = 0f,
            HeatDecaysMidAir = false,
            FloorSpacing = Tuning.FloorSpacing,
            SmashCostMultiplier = 1f,
            BaseScrollSpeed = Tuning.EmberScrollSpeed,
            ScrollSpeedPerMeter = 0f,
            MaxScrollSpeed = Tuning.EmberScrollSpeed,
            SurgeEnabled = false,
            HeatAffectsScore = false,
            AdrenalineEnabled = false,
            Music = StemMix.PadOnly,
        },
        _ => new ModeSettings
        {
            CrusherEnabled = true,
            DurationLimitSeconds = 0f,
            HeatDecaysMidAir = false,
            FloorSpacing = Tuning.FloorSpacing,
            SmashCostMultiplier = 1f,
            BaseScrollSpeed = Tuning.BaseScrollSpeed,
            ScrollSpeedPerMeter = Tuning.ScrollSpeedPerMeter,
            MaxScrollSpeed = Tuning.MaxScrollSpeed,
            SurgeEnabled = true,
            HeatAffectsScore = true,
            AdrenalineEnabled = true,
            Music = StemMix.Full,
        },
    };
}

/// <summary>
/// Display names and Ready-screen descriptions for each mode.
/// </summary>
public static class ModeCatalog
{
    /// <summary>Every selectable mode, in Ready-screen cycle order.</summary>
    public static readonly GameMode[] All =
        [GameMode.Freefall, GameMode.DailyInferno, GameMode.EmberGarden];

    /// <summary>
    /// Gets a mode's display name.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The uppercase display name.</returns>
    public static string NameOf(GameMode mode) => mode switch
    {
        GameMode.DailyInferno => "DAILY INFERNO",
        GameMode.EmberGarden => "EMBER GARDEN",
        _ => "FREEFALL",
    };

    /// <summary>
    /// Gets a mode's one-line Ready-screen description.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The description text.</returns>
    public static string DescriptionOf(GameMode mode) => mode switch
    {
        GameMode.DailyInferno => "3 minutes - today's seed - heat bleeds mid-air - 3 attempts",
        GameMode.EmberGarden => "no furnace, no death - a gentle shaft to feel the heat in",
        _ => "endless descent - the Furnace never stops",
    };
}

/// <summary>
/// The Daily Inferno schedule: date-hashed seeds and depth medals. Pure
/// functions, so the test project can pin them down.
/// </summary>
public static class DailySchedule
{
    /// <summary>
    /// Packs a date into its integer key, e.g. 2026-07-24 becomes 20260724.
    /// Used both for seeding and as the save-file key for attempts and medals.
    /// </summary>
    /// <param name="date">The calendar date.</param>
    /// <returns>The yyyyMMdd integer key.</returns>
    public static int DateKey(DateOnly date) => date.Year * 10000 + date.Month * 100 + date.Day;

    /// <summary>
    /// Derives the run seed for a date: the yyyyMMdd key pushed through one
    /// SplitMix64 step, so consecutive days get well-scattered, stable seeds.
    /// </summary>
    /// <param name="date">The calendar date.</param>
    /// <returns>The seed every player gets for that date.</returns>
    public static ulong SeedForDate(DateOnly date) => SeededGenerator.NextSeed((ulong)DateKey(date));

    /// <summary>
    /// Gets the medal earned for a final depth: 0 none, 1 bronze, 2 silver, 3 gold.
    /// </summary>
    /// <param name="depthMeters">The run's final depth in meters.</param>
    /// <returns>The medal index.</returns>
    public static int MedalForDepth(float depthMeters)
    {
        var medal = 0;
        for (var i = 0; i < Tuning.DailyMedalDepths.Length; i++)
        {
            if (depthMeters >= Tuning.DailyMedalDepths[i])
            {
                medal = i + 1;
            }
        }

        return medal;
    }

    /// <summary>
    /// Gets a medal's display name ("-", "BRONZE", "SILVER", "GOLD").
    /// </summary>
    /// <param name="medal">The medal index from <see cref="MedalForDepth"/>.</param>
    /// <returns>The display name.</returns>
    public static string MedalName(int medal) => medal switch
    {
        3 => "GOLD",
        2 => "SILVER",
        1 => "BRONZE",
        _ => "-",
    };
}

// ============================================================================
// Phase C simulation singletons.
// ============================================================================

/// <summary>
/// The music clock: scaled-simulation seconds since the run started. Pulse
/// floors, the Flashover Surge window, and the Daily Inferno time limit all
/// read this — never the wall clock — so every beat-synced mechanic replays
/// identically in headless <c>--simulate</c> mode.
/// </summary>
public struct MusicClock
{
    /// <summary>Scaled seconds since the run started.</summary>
    public float Seconds;
}

/// <summary>
/// Flashover Surge state. A surge triggers every <see cref="Tuning.SurgePeriodFloors"/>
/// cleared floors — a pure function of the floor script, never of time.
/// </summary>
public struct SurgeState
{
    /// <summary>True while a surge window is open.</summary>
    public bool Active;

    /// <summary>Music-clock time at which the active surge ends.</summary>
    public float EndsAtSeconds;

    /// <summary>Deepest floor index the ball has cleanly cleared this run.</summary>
    public int DeepestClearedFloor;

    /// <summary>Floor index whose clearing triggers the next surge.</summary>
    public int NextSurgeFloor;

    /// <summary>Floor Smashes inside the current surge window.</summary>
    public int SmashesThisSurge;

    /// <summary>True once this window's Surge Sweep bonus has been paid.</summary>
    public bool SweepAwarded;
}

/// <summary>
/// Adrenaline Save state: the once-per-run last-chance slow-motion window.
/// </summary>
public struct AdrenalineState
{
    /// <summary>True until the run's single save has been spent.</summary>
    public bool Available;

    /// <summary>True while the save window is open.</summary>
    public bool Active;

    /// <summary>REAL seconds left in the window (raw delta time, see <see cref="Tuning.AdrenalineRealSeconds"/>).</summary>
    public float RealSecondsRemaining;
}

/// <summary>
/// Which Ready-screen row the Left/Right axis currently cycles.
/// </summary>
public enum MenuRow
{
    /// <summary>Left/Right cycles the game mode.</summary>
    Mode,

    /// <summary>Left/Right cycles the cosmetic style.</summary>
    Cosmetics,
}

/// <summary>
/// Ready-screen menu state. Input stays one-axis even here: Left/Right cycles
/// the active row, Tab (the one non-axis concession) switches rows, and
/// Space/Enter dives.
/// </summary>
public struct MenuState
{
    /// <summary>The row Left/Right currently cycles.</summary>
    public MenuRow Row;

    /// <summary>The mode the menu currently shows (applied on start).</summary>
    public GameMode SelectedMode;

    /// <summary>Why the last run ended, shown on the death card ("crushed" vs "time").</summary>
    public bool LastRunTimedOut;
}
