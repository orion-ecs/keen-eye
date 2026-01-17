namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Result of checking replay determinism.
/// </summary>
public sealed record DeterminismResultSnapshot
{
    /// <summary>
    /// Gets whether the replay is deterministic.
    /// </summary>
    public required bool IsDeterministic { get; init; }

    /// <summary>
    /// Gets the total frames checked.
    /// </summary>
    public required int TotalFramesChecked { get; init; }

    /// <summary>
    /// Gets the number of frames with checksums.
    /// </summary>
    public required int FramesWithChecksums { get; init; }

    /// <summary>
    /// Gets the first frame where a desync was detected, if any.
    /// </summary>
    public int? FirstDesyncFrame { get; init; }

    /// <summary>
    /// Gets the expected checksum at the desync frame.
    /// </summary>
    public uint? ExpectedChecksum { get; init; }

    /// <summary>
    /// Gets the actual checksum at the desync frame.
    /// </summary>
    public uint? ActualChecksum { get; init; }

    /// <summary>
    /// Gets details about the desync, if any.
    /// </summary>
    public string? DesyncDetails { get; init; }
}
