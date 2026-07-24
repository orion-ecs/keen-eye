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
public struct RunConfig
{
    /// <summary>Seed driving all floor generation for this run.</summary>
    public ulong Seed;

    /// <summary>
    /// When true, restarts reuse <see cref="Seed"/> instead of deriving a new one.
    /// Useful for practicing a specific layout and for deterministic testing.
    /// </summary>
    public bool PinSeed;
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
/// Per-frame collision events published by <see cref="CollisionSystem"/> and
/// consumed (then cleared) by <see cref="HeatSystem"/>.
/// </summary>
public struct FrameEvents
{
    /// <summary>Number of floors the ball passed cleanly through this frame.</summary>
    public int GapsPassed;

    /// <summary>True if the ball landed on a floor this frame.</summary>
    public bool Landed;
}
