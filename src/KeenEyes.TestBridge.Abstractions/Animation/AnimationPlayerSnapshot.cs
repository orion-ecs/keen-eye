namespace KeenEyes.TestBridge.Animation;

/// <summary>
/// Snapshot of an animation player component state.
/// </summary>
public sealed record AnimationPlayerSnapshot
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets or sets the animation clip ID.
    /// </summary>
    public required int ClipId { get; init; }

    /// <summary>
    /// Gets or sets the current playback time in seconds.
    /// </summary>
    public required float Time { get; init; }

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public required float Speed { get; init; }

    /// <summary>
    /// Gets or sets whether the animation is currently playing.
    /// </summary>
    public required bool IsPlaying { get; init; }

    /// <summary>
    /// Gets or sets whether the animation has completed (for non-looping animations).
    /// </summary>
    public required bool IsComplete { get; init; }

    /// <summary>
    /// Gets or sets the blend weight for this animation (0-1).
    /// </summary>
    public required float Weight { get; init; }

    /// <summary>
    /// Gets or sets the wrap mode override, if any.
    /// </summary>
    public required string? WrapModeOverride { get; init; }
}
