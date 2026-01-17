namespace KeenEyes.TestBridge.Animation;

/// <summary>
/// Snapshot of animation system statistics.
/// </summary>
public sealed record AnimationStatisticsSnapshot
{
    /// <summary>
    /// Gets or sets the number of registered animation clips.
    /// </summary>
    public required int ClipCount { get; init; }

    /// <summary>
    /// Gets or sets the number of registered animator controllers.
    /// </summary>
    public required int ControllerCount { get; init; }

    /// <summary>
    /// Gets or sets the number of registered sprite sheets.
    /// </summary>
    public required int SpriteSheetCount { get; init; }

    /// <summary>
    /// Gets or sets the number of entities with active animation players.
    /// </summary>
    public required int ActivePlayerCount { get; init; }

    /// <summary>
    /// Gets or sets the number of entities with active animators.
    /// </summary>
    public required int ActiveAnimatorCount { get; init; }
}
