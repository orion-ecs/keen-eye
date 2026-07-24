using System.Numerics;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Central gameplay tuning constants for NOVAFALL.
/// </summary>
/// <remarks>
/// <para>
/// All simulation values are expressed in a fixed <em>design space</em> of
/// <see cref="ShaftWidth"/> x <see cref="ShaftHeight"/> units, independent of the
/// actual window size. The render system scales design units to pixels, which keeps
/// the simulation fully deterministic and lets the headless <c>--simulate</c> mode
/// replay a run without any window at all.
/// </para>
/// <para>
/// Keeping every knob in one file makes the game feel easy to iterate on and gives
/// readers a single map of the mechanics.
/// </para>
/// </remarks>
public static class Tuning
{
    // --- Shaft geometry (design units) ---

    /// <summary>Width of the playfield shaft in design units.</summary>
    public const float ShaftWidth = 720f;

    /// <summary>Height of the playfield shaft in design units.</summary>
    public const float ShaftHeight = 1080f;

    /// <summary>Y position of the Furnace ceiling. Touching it is death.</summary>
    public const float CeilingY = 80f;

    /// <summary>Design units per scored meter of descent.</summary>
    public const float UnitsPerMeter = 100f;

    // --- Ball ---

    /// <summary>Radius of the ball in design units.</summary>
    public const float BallRadius = 16f;

    /// <summary>Ball spawn X at the start of a run (shaft center).</summary>
    public const float BallSpawnX = ShaftWidth / 2f;

    /// <summary>Ball spawn Y at the start of a run.</summary>
    public const float BallSpawnY = 240f;

    /// <summary>Downward gravity in design units per second squared.</summary>
    public const float Gravity = 1500f;

    /// <summary>Terminal fall speed in design units per second.</summary>
    public const float MaxFallSpeed = 1100f;

    /// <summary>Horizontal steering acceleration in design units per second squared.</summary>
    public const float SteerAcceleration = 2800f;

    /// <summary>Maximum horizontal speed in design units per second.</summary>
    public const float MaxHorizontalSpeed = 420f;

    /// <summary>Horizontal damping factor per second while resting on a floor with no input.</summary>
    public const float GroundFriction = 8f;

    /// <summary>Horizontal damping factor per second while airborne with no input.</summary>
    public const float AirDrag = 1.5f;

    // --- Floors ---

    /// <summary>Vertical thickness of each floor in design units.</summary>
    public const float FloorThickness = 24f;

    /// <summary>Vertical distance between consecutive floors in design units.</summary>
    public const float FloorSpacing = 170f;

    /// <summary>Y position of the first floor of a run.</summary>
    public const float FirstFloorY = 430f;

    /// <summary>Minimum gap width in design units (comfortably wider than the ball).</summary>
    public const float GapWidthMin = 96f;

    /// <summary>Maximum gap width in design units.</summary>
    public const float GapWidthMax = 150f;

    /// <summary>Minimum distance from a gap edge to a shaft wall.</summary>
    public const float GapWallMargin = 40f;

    /// <summary>How far above the ceiling a floor may scroll before it is despawned.</summary>
    public const float FloorDespawnMargin = 60f;

    /// <summary>Extra tolerance below a floor top that still counts as a landing.</summary>
    public const float LandingTolerance = 14f;

    // --- Scrolling ---

    /// <summary>Upward scroll speed at depth zero, in design units per second.</summary>
    public const float BaseScrollSpeed = 70f;

    /// <summary>Additional scroll speed per meter of depth.</summary>
    public const float ScrollSpeedPerMeter = 1.1f;

    /// <summary>Upper bound on scroll speed in design units per second.</summary>
    public const float MaxScrollSpeed = 340f;

    // --- Heat ---

    /// <summary>Heat gained for each clean gap-through.</summary>
    public const float HeatPerGap = 12f;

    /// <summary>Heat lost per second while resting on a floor.</summary>
    public const float HeatDecayPerSecond = 3f;

    /// <summary>Maximum heat value.</summary>
    public const float MaxHeat = 200f;

    /// <summary>Heat required for the Flame tier (x2 multiplier).</summary>
    public const float FlameThreshold = 30f;

    /// <summary>Heat required for the Plasma tier (x4 multiplier).</summary>
    public const float PlasmaThreshold = 70f;

    /// <summary>Heat required for the Nova tier (x8 multiplier).</summary>
    public const float NovaThreshold = 120f;

    // --- Flow ---

    /// <summary>Seconds after death before a restart key press is accepted.</summary>
    public const float RestartCooldown = 0.75f;

    // --- Floor Smash (Phase B gameplay) ---

    /// <summary>Minimum heat tier (Plasma) at which a landing becomes a Floor Smash.</summary>
    public const int SmashMinTier = 2;

    /// <summary>Fraction of fall speed kept after smashing through a floor.</summary>
    public const float SmashFallRetention = 0.45f;

    /// <summary>Score bonus for a Floor Smash.</summary>
    public const int SmashScoreBonus = 250;

    /// <summary>Simulation frames frozen by the smash hitstop.</summary>
    public const int SmashHitstopFrames = 4;

    // --- Graze Sparks (Phase B gameplay) ---

    /// <summary>Maximum clearance from a gap edge that still counts as a graze.</summary>
    public const float GrazeDistance = 12f;

    /// <summary>Score bonus for each graze.</summary>
    public const int GrazeScoreBonus = 50;

    /// <summary>Heat gained per graze.</summary>
    public const float GrazeHeatBonus = 4f;

    /// <summary>Radius of the quadtree proximity query used for graze detection.</summary>
    public const float GrazeQueryRadius = 420f;

    // --- Camera (Phase B juice) ---

    /// <summary>Trauma added by a Floor Smash.</summary>
    public const float SmashTrauma = 0.55f;

    /// <summary>Trauma added by a graze.</summary>
    public const float GrazeTrauma = 0.12f;

    /// <summary>Trauma added by a heat tier promotion.</summary>
    public const float TierUpTrauma = 0.30f;

    /// <summary>Trauma drained per real-time second.</summary>
    public const float TraumaDecayPerSecond = 1.6f;

    /// <summary>Maximum shake offset in design units (kept small for readability).</summary>
    public const float MaxShakeOffset = 6f;

    /// <summary>Shake noise frequency in hertz.</summary>
    public const float ShakeFrequency = 26f;

    /// <summary>Downward camera kick applied by a Floor Smash, in design units.</summary>
    public const float SmashKick = 26f;

    /// <summary>Kick recovery rate (exponential decay per second).</summary>
    public const float KickDecayPerSecond = 7f;

    /// <summary>Zoom factor at terminal fall speed (slightly zoomed out).</summary>
    public const float ZoomOutAtMaxFall = 0.94f;

    /// <summary>Zoom factor when the ball is about to be crushed (zoomed in).</summary>
    public const float CrushZoomIn = 1.06f;

    /// <summary>Distance from the ceiling at which crush zoom starts building.</summary>
    public const float CrushProximityRange = 220f;

    /// <summary>Zoom smoothing rate per second.</summary>
    public const float ZoomLerpPerSecond = 5f;

    // --- Trail and glow (Phase B juice) ---

    /// <summary>Capacity of the comet trail ring buffer.</summary>
    public const int TrailCapacity = 26;

    /// <summary>Trail points drawn at Ember tier; each tier adds <see cref="TrailPointsPerTier"/>.</summary>
    public const int TrailBasePoints = 8;

    /// <summary>Additional trail points per heat tier.</summary>
    public const int TrailPointsPerTier = 6;

    /// <summary>Trail ribbon width at Ember tier, in design units.</summary>
    public const float TrailBaseWidth = 5f;

    /// <summary>Additional ribbon width per heat tier.</summary>
    public const float TrailWidthPerTier = 3f;

    /// <summary>Glow pulse frequency in hertz.</summary>
    public const float GlowPulseHz = 2.2f;

    // --- Palette (Phase B juice) ---

    /// <summary>Duration of the palette cross-tween on a tier change, in seconds.</summary>
    public const float PaletteTweenSeconds = 0.6f;

    /// <summary>
    /// The four tier palettes: slate, amber, magenta-violet, and blue-white, all
    /// over dark navy-to-black backgrounds so the additive glow reads.
    /// Indexed by <see cref="HeatState.Tier"/>.
    /// </summary>
    public static readonly Palette[] TierPalettes =
    [
        new Palette // Ember — slate
        {
            Background = new Vector4(0.030f, 0.045f, 0.100f, 1f),
            FloorFill = new Vector4(0.160f, 0.200f, 0.320f, 1f),
            FloorOutline = new Vector4(0.450f, 0.560f, 0.800f, 1f),
            Ball = new Vector4(0.950f, 0.550f, 0.200f, 1f),
            Trail = new Vector4(0.950f, 0.450f, 0.150f, 1f),
            UiAccent = new Vector4(0.750f, 0.800f, 0.950f, 1f),
        },
        new Palette // Flame — amber
        {
            Background = new Vector4(0.060f, 0.032f, 0.055f, 1f),
            FloorFill = new Vector4(0.250f, 0.140f, 0.100f, 1f),
            FloorOutline = new Vector4(0.950f, 0.620f, 0.250f, 1f),
            Ball = new Vector4(1.000f, 0.450f, 0.120f, 1f),
            Trail = new Vector4(1.000f, 0.550f, 0.100f, 1f),
            UiAccent = new Vector4(1.000f, 0.720f, 0.300f, 1f),
        },
        new Palette // Plasma — magenta-violet
        {
            Background = new Vector4(0.050f, 0.020f, 0.100f, 1f),
            FloorFill = new Vector4(0.200f, 0.100f, 0.300f, 1f),
            FloorOutline = new Vector4(0.800f, 0.450f, 1.000f, 1f),
            Ball = new Vector4(0.780f, 0.420f, 1.000f, 1f),
            Trail = new Vector4(0.850f, 0.350f, 1.000f, 1f),
            UiAccent = new Vector4(0.880f, 0.600f, 1.000f, 1f),
        },
        new Palette // Nova — blue-white
        {
            Background = new Vector4(0.020f, 0.040f, 0.090f, 1f),
            FloorFill = new Vector4(0.120f, 0.200f, 0.340f, 1f),
            FloorOutline = new Vector4(0.700f, 0.850f, 1.000f, 1f),
            Ball = new Vector4(0.880f, 0.960f, 1.000f, 1f),
            Trail = new Vector4(0.750f, 0.900f, 1.000f, 1f),
            UiAccent = new Vector4(0.920f, 0.970f, 1.000f, 1f),
        },
    ];

    // --- Particles (Phase B juice) ---

    /// <summary>Fragments spawned by a Floor Smash.</summary>
    public const int SmashFragmentCount = 26;

    /// <summary>Sparks in the radial ring spawned by a Floor Smash.</summary>
    public const int SmashSparkCount = 30;

    /// <summary>Sparks spawned by a graze.</summary>
    public const int GrazeSparkCount = 14;

    /// <summary>Embers spawned by the death shatter.</summary>
    public const int DeathEmberCount = 60;

    /// <summary>
    /// Hard cap on live burst-effect entities. Part of the readability contract:
    /// the shaft never drowns in particles near the floor rows. When the cap is
    /// hit the oldest burst is despawned first — the fragment-budget / pooling
    /// lesson in miniature.
    /// </summary>
    public const int MaxLiveBursts = 10;

    // --- Combo toasts (Phase B juice) ---

    /// <summary>Combo counts at which the toast ladder fires (NICE and up).</summary>
    public static readonly int[] ComboToastThresholds = [3, 6, 10, 15];

    /// <summary>Toast texts matching <see cref="ComboToastThresholds"/>.</summary>
    public static readonly string[] ComboToastTexts = ["NICE", "BLAZING", "INCANDESCENT", "SUPERNOVA"];

    /// <summary>Seconds a combo toast stays on screen.</summary>
    public const float ToastSeconds = 1.4f;

    // --- Audio (Phase B juice) ---

    /// <summary>Real-time seconds for a full music stem cross-fade.</summary>
    public const float StemFadePerSecond = 1.8f;

    /// <summary>Wind loop pitch at zero fall speed.</summary>
    public const float WindPitchMin = 0.55f;

    /// <summary>Wind loop pitch at terminal fall speed.</summary>
    public const float WindPitchMax = 1.55f;

    /// <summary>Pitch step added to the graze ting per consecutive graze.</summary>
    public const float GrazePitchStep = 0.09f;

    /// <summary>Consecutive grazes after which the ting pitch stops rising.</summary>
    public const int GrazePitchCap = 8;

    // --- Death beat (Phase B juice) ---

    /// <summary>Simulation time scale during the death slow-motion beat.</summary>
    public const float DeathSlowMo = 0.2f;

    /// <summary>Real seconds after death at which all audio stops.</summary>
    public const float DeathSilenceStart = 0.4f;

    /// <summary>Real seconds of true silence before the score card appears.</summary>
    public const float DeathSilenceSeconds = 0.4f;

    /// <summary>Real seconds the white death flash takes to fade.</summary>
    public const float DeathFlashSeconds = 0.35f;

    /// <summary>Real seconds after death at which normal time resumes.</summary>
    public const float DeathSlowMoSeconds = 0.5f;
}
