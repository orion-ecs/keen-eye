namespace KeenEyes.TestBridge.Animation;

/// <summary>
/// Snapshot of an animator component state.
/// </summary>
public sealed record AnimatorSnapshot
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets or sets the animator controller ID.
    /// </summary>
    public required int ControllerId { get; init; }

    /// <summary>
    /// Gets or sets the current state hash.
    /// </summary>
    public required int CurrentStateHash { get; init; }

    /// <summary>
    /// Gets or sets the current state name, if available.
    /// </summary>
    public required string? CurrentStateName { get; init; }

    /// <summary>
    /// Gets or sets the playback time within the current state.
    /// </summary>
    public required float StateTime { get; init; }

    /// <summary>
    /// Gets or sets whether the animator is currently transitioning between states.
    /// </summary>
    public required bool IsTransitioning { get; init; }

    /// <summary>
    /// Gets or sets the transition progress (0-1) when transitioning.
    /// </summary>
    public required float TransitionProgress { get; init; }

    /// <summary>
    /// Gets or sets the global speed multiplier.
    /// </summary>
    public required float Speed { get; init; }

    /// <summary>
    /// Gets or sets whether the animator is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the next state hash during transitions, or 0 if not transitioning.
    /// </summary>
    public required int NextStateHash { get; init; }

    /// <summary>
    /// Gets or sets the next state name during transitions, if available.
    /// </summary>
    public required string? NextStateName { get; init; }
}
