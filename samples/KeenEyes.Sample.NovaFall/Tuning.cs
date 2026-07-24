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
}
