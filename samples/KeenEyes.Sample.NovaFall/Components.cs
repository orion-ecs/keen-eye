namespace KeenEyes.Sample.NovaFall;

// ============================================================================
// Entity components — pure data attached to entities.
// ============================================================================

/// <summary>
/// Marks the player-controlled ball and stores its collision radius.
/// </summary>
[Component]
public partial struct Ball
{
    /// <summary>Collision radius in design units.</summary>
    public float Radius;
}

/// <summary>
/// 2D position in design-space units. (0,0) is the top-left of the shaft,
/// Y increases downward.
/// </summary>
[Component]
public partial struct Position2D
{
    /// <summary>Horizontal position.</summary>
    public float X;

    /// <summary>Vertical position.</summary>
    public float Y;
}

/// <summary>
/// 2D velocity in design-space units per second.
/// </summary>
[Component]
public partial struct Velocity2D
{
    /// <summary>Horizontal velocity.</summary>
    public float X;

    /// <summary>Vertical velocity (positive is downward).</summary>
    public float Y;
}

/// <summary>
/// Steering input for the ball, written by <see cref="InputSteerSystem"/> and
/// consumed by <see cref="BallMovementSystem"/>.
/// </summary>
/// <remarks>
/// Storing the input on the entity (instead of having the movement system poll
/// devices directly) keeps device access in exactly one system, so the rest of the
/// simulation stays headless-friendly and deterministic.
/// </remarks>
[Component]
public partial struct SteerInput
{
    /// <summary>Steering axis in [-1, 1]; negative steers left, positive steers right.</summary>
    public float Axis;
}

/// <summary>
/// The personality of a floor. Standard floors are plain slabs; the other kinds
/// phase in by depth (see <see cref="FloorLayout.KindForFloor"/>) as minority spice.
/// </summary>
public enum FloorKind
{
    /// <summary>A plain slab with a gap.</summary>
    Standard,

    /// <summary>Cracks when landed on, then crumbles into fragments after a telegraphed delay.</summary>
    Brittle,

    /// <summary>Launches the ball back upward with an elastic impulse instead of catching it.</summary>
    Bumper,

    /// <summary>Its gap phases open and closed on the music beat, telegraphing each close.</summary>
    Pulse,
}

/// <summary>
/// A horizontal floor spanning the shaft, with a single gap the ball can fall through.
/// </summary>
[Component]
public partial struct Floor
{
    /// <summary>Sequential index of this floor within the run (0 = first floor).</summary>
    public int Index;

    /// <summary>X position of the gap's center.</summary>
    public float GapCenterX;

    /// <summary>Width of the gap.</summary>
    public float GapWidth;

    /// <summary>Vertical thickness of the floor slab.</summary>
    public float Thickness;

    /// <summary>
    /// True once the ball has passed below this floor. Prevents a single floor
    /// from stoking heat more than once.
    /// </summary>
    public bool Cleared;

    /// <summary>The floor's personality.</summary>
    public FloorKind Kind;

    /// <summary>True once a Brittle floor has been landed on and started cracking.</summary>
    public bool Cracking;

    /// <summary>Scaled seconds since a Brittle floor started cracking.</summary>
    public float CrackSeconds;

    /// <summary>Seconds of bounce-ease wobble left on a Bumper floor after a launch.</summary>
    public float WobbleSeconds;
}

/// <summary>
/// Present on the ball while it rests on a floor; the floor carries it upward.
/// </summary>
[Component]
public partial struct RestingOn
{
    /// <summary>The floor entity the ball is resting on.</summary>
    public Entity FloorEntity;
}

// ============================================================================
// Singletons — world-wide state accessed via the World singleton API.
// ============================================================================

/// <summary>
/// Immutable-per-run configuration for the current run.
/// </summary>
/// <remarks>
/// Modes are configuration, not code paths: FREEFALL, DAILY INFERNO, and EMBER
/// GARDEN all run the exact same systems, differing only in the knobs captured
/// in <see cref="Settings"/> when the run starts.
/// </remarks>
public struct RunConfig
{
    /// <summary>Seed driving all floor generation for this run.</summary>
    public ulong Seed;

    /// <summary>
    /// When true, restarts reuse <see cref="Seed"/> instead of deriving a new one.
    /// Useful for practicing a specific layout and for deterministic testing.
    /// </summary>
    public bool PinSeed;

    /// <summary>The mode this run belongs to.</summary>
    public GameMode Mode;

    /// <summary>The mode's knob values, captured from <see cref="ModeSettings.For"/>.</summary>
    public ModeSettings Settings;
}

/// <summary>
/// State of the upward-scrolling shaft.
/// </summary>
public struct ScrollState
{
    /// <summary>Current upward scroll speed in design units per second.</summary>
    public float Speed;

    /// <summary>Total depth fallen this run, in meters.</summary>
    public float Depth;

    /// <summary>Index of the next floor to spawn (also the count of floors spawned).</summary>
    public int NextFloorIndex;
}

/// <summary>
/// The Heat resource stoked by clean gap-throughs.
/// </summary>
public struct HeatState
{
    /// <summary>Current heat in [0, <see cref="Tuning.MaxHeat"/>].</summary>
    public float Heat;

    /// <summary>Current tier: 0 = Ember, 1 = Flame, 2 = Plasma, 3 = Nova.</summary>
    public int Tier;
}

/// <summary>
/// Score accumulation for the current run and the best score across runs.
/// </summary>
public struct ScoreState
{
    /// <summary>Score for the current run (meters fallen x heat multiplier, integrated).</summary>
    public double Score;

    /// <summary>Best final score across all runs this session.</summary>
    public int Best;

    /// <summary>Depth already converted into score, used to integrate per-frame deltas.</summary>
    public float LastDepth;
}

/// <summary>
/// The high-level phase of the game.
/// </summary>
public enum GamePhase
{
    /// <summary>Waiting for the player to start.</summary>
    Ready,

    /// <summary>The run is live.</summary>
    Playing,

    /// <summary>The ball touched the Furnace ceiling.</summary>
    Dead,
}

/// <summary>
/// Current <see cref="GamePhase"/> of the game.
/// </summary>
public struct GameState
{
    /// <summary>The active phase.</summary>
    public GamePhase Phase;
}

/// <summary>
/// Global simulation time multiplier. Every simulation system multiplies its delta
/// time by <see cref="Value"/>, providing the hook for hitstop and slow-motion
/// effects in later phases.
/// </summary>
public struct TimeScale
{
    /// <summary>Multiplier applied to simulation delta time (1 = normal speed).</summary>
    public float Value;
}

/// <summary>
/// Per-frame gameplay events published by the simulation systems.
/// </summary>
/// <remarks>
/// Events published during one frame's Update phase are consumed by simulation
/// systems the same frame (heat, score) and by juice systems either the same
/// frame (particles, audio) or at the start of the next frame (camera trauma,
/// hitstop). <see cref="FrameEventsClearSystem"/> zeroes the struct in the next
/// frame's EarlyUpdate, after every consumer has seen it exactly once.
/// </remarks>
public struct FrameEvents
{
    /// <summary>Number of floors the ball passed cleanly through this frame.</summary>
    public int GapsPassed;

    /// <summary>True if the ball landed on a floor this frame.</summary>
    public bool Landed;

    /// <summary>Fall speed at the moment of landing, for squash scaling.</summary>
    public float LandingSpeed;

    /// <summary>True if the ball smashed through a floor this frame.</summary>
    public bool Smashed;

    /// <summary>Ball X at the moment of the smash.</summary>
    public float SmashX;

    /// <summary>Top Y of the smashed floor.</summary>
    public float SmashY;

    /// <summary>Fall speed at the moment of the smash, for crunch pitch and kick.</summary>
    public float SmashImpactSpeed;

    /// <summary>Gap center X of the smashed floor, for fragment placement.</summary>
    public float SmashGapCenterX;

    /// <summary>Gap width of the smashed floor, for fragment placement.</summary>
    public float SmashGapWidth;

    /// <summary>Number of grazes scored this frame (0 or 1 in practice).</summary>
    public int Grazes;

    /// <summary>Ball X at the moment of the last graze this frame.</summary>
    public float GrazeX;

    /// <summary>Ball Y at the moment of the last graze this frame.</summary>
    public float GrazeY;

    /// <summary>True if the heat tier changed this frame.</summary>
    public bool TierChanged;

    /// <summary>Tier before the change (valid when <see cref="TierChanged"/>).</summary>
    public int TierFrom;

    /// <summary>Tier after the change (valid when <see cref="TierChanged"/>).</summary>
    public int TierTo;

    /// <summary>True if a Brittle floor started cracking this frame (the telegraph).</summary>
    public bool CrackStarted;

    /// <summary>Ball X at the moment the crack started.</summary>
    public float CrackX;

    /// <summary>Top Y of the cracking floor.</summary>
    public float CrackY;

    /// <summary>True if a Brittle floor crumbled this frame.</summary>
    public bool Crumbled;

    /// <summary>Top Y of the crumbled floor.</summary>
    public float CrumbleY;

    /// <summary>Gap center X of the crumbled floor, for fragment placement.</summary>
    public float CrumbleGapCenterX;

    /// <summary>Gap width of the crumbled floor, for fragment placement.</summary>
    public float CrumbleGapWidth;

    /// <summary>True if a Bumper floor launched the ball this frame.</summary>
    public bool Bumped;

    /// <summary>Ball X at the moment of the bumper launch.</summary>
    public float BumpX;

    /// <summary>Top Y of the launching Bumper floor.</summary>
    public float BumpY;

    /// <summary>Fall speed at the moment of the bumper launch, for boing pitch.</summary>
    public float BumpImpactSpeed;

    /// <summary>True if a Flashover Surge began this frame.</summary>
    public bool SurgeStarted;

    /// <summary>True if the active Flashover Surge ended this frame.</summary>
    public bool SurgeEnded;

    /// <summary>True if the Surge Sweep bonus was earned this frame.</summary>
    public bool SurgeSweepAwarded;

    /// <summary>True if the Adrenaline Save triggered this frame.</summary>
    public bool AdrenalineTriggered;

    /// <summary>True if the ball escaped the crush zone and survived its Adrenaline Save.</summary>
    public bool AdrenalineSurvived;
}

/// <summary>
/// Floor Smash bookkeeping: which floor was smashed last, so a smash can never
/// trigger on two consecutive floors.
/// </summary>
public struct SmashState
{
    /// <summary>Index of the most recently smashed floor, or a large negative value.</summary>
    public int LastSmashedFloorIndex;
}

/// <summary>
/// Combo tracking: consecutive clean gap-throughs without a landing, plus the
/// current consecutive-graze chain that drives the rising ting pitch.
/// </summary>
public struct ComboState
{
    /// <summary>Consecutive clean gap-throughs since the last landing.</summary>
    public int Combo;

    /// <summary>Consecutive grazes since the last landing.</summary>
    public int ConsecutiveGrazes;

    /// <summary>Highest combo reached this run, for cosmetic unlock milestones.</summary>
    public int MaxCombo;
}

/// <summary>
/// Deterministic per-run event counters, printed by the headless
/// <c>--simulate</c> mode as part of the determinism guard.
/// </summary>
public struct RunEventCounters
{
    /// <summary>Total Floor Smashes this run.</summary>
    public int Smashes;

    /// <summary>Total grazes this run.</summary>
    public int Grazes;

    /// <summary>Total Brittle floor crumbles this run.</summary>
    public int Crumbles;

    /// <summary>Total Bumper launches this run.</summary>
    public int Bumps;

    /// <summary>Flashover Surge windows entered this run.</summary>
    public int SurgeWindows;

    /// <summary>Adrenaline Saves used this run (0 or 1).</summary>
    public int AdrenalineSavesUsed;
}

/// <summary>
/// Hitstop bookkeeping owned by <see cref="HitstopSystem"/>. Counted in frames
/// (real time), not scaled time — a frozen clock cannot be used to unfreeze itself.
/// </summary>
public struct HitstopState
{
    /// <summary>Frames of hitstop remaining; the system restores time at zero.</summary>
    public int FramesRemaining;
}
