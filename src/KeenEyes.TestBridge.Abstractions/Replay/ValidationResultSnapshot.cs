namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Result of validating a replay file.
/// </summary>
public sealed record ValidationResultSnapshot
{
    /// <summary>
    /// Gets whether the file is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the file path that was validated.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the total number of frames in the file.
    /// </summary>
    public int? TotalFrames { get; init; }

    /// <summary>
    /// Gets the number of snapshots in the file.
    /// </summary>
    public int? SnapshotCount { get; init; }

    /// <summary>
    /// Gets the data version of the replay file.
    /// </summary>
    public int? DataVersion { get; init; }

    /// <summary>
    /// Gets any validation errors found.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; init; }
}
