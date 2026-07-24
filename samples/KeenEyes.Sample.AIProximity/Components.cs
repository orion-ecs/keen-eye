using System.Numerics;

namespace KeenEyes.Sample.AIProximity;

/// <summary>
/// Guard AI state machine.
/// </summary>
public enum GuardState
{
    /// <summary>Patrolling, no threats detected.</summary>
    Idle,

    /// <summary>Heard something, investigating.</summary>
    Searching,

    /// <summary>Saw player, actively pursuing.</summary>
    Alert
}

/// <summary>
/// Component for guard AI agents.
/// </summary>
[Component]
public partial struct Guard
{
    /// <summary>Maximum distance at which the guard can see a player.</summary>
    public float VisionRange;

    /// <summary>Maximum distance at which the guard can hear a noisy player.</summary>
    public float HearingRange;

    /// <summary>Distance within which the guard broadcasts alerts to other guards.</summary>
    public float AlertRange;

    /// <summary>Current AI state.</summary>
    public GuardState State;

    /// <summary>Seconds remaining before a searching guard returns to idle.</summary>
    public float SearchTimer;
}

/// <summary>
/// Component for entity velocity.
/// </summary>
[Component]
public partial struct Velocity
{
    /// <summary>Velocity vector in world units per second.</summary>
    public Vector3 Value;
}

/// <summary>
/// Component for noise generation (affects hearing detection).
/// </summary>
[Component]
public partial struct Noisy
{
    /// <summary>Noise level from 0.0 (silent) to 1.0 (very loud).</summary>
    public float NoiseLevel;
}

/// <summary>
/// Tag component for player entities.
/// </summary>
[TagComponent]
public partial struct Player;
