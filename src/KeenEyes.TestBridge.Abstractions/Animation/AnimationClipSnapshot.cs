namespace KeenEyes.TestBridge.Animation;

/// <summary>
/// Snapshot of an animation clip asset.
/// </summary>
public sealed record AnimationClipSnapshot
{
    /// <summary>
    /// Gets or sets the clip ID.
    /// </summary>
    public required int ClipId { get; init; }

    /// <summary>
    /// Gets or sets the clip name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    public required float Duration { get; init; }

    /// <summary>
    /// Gets or sets the wrap mode.
    /// </summary>
    public required string WrapMode { get; init; }

    /// <summary>
    /// Gets or sets the number of bone tracks in this clip.
    /// </summary>
    public required int BoneTrackCount { get; init; }
}
