namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Result of a replay operation.
/// </summary>
public sealed record ReplayOperationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the file path involved in the operation, if any.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets recording info if available after the operation.
    /// </summary>
    public RecordingInfoSnapshot? RecordingInfo { get; init; }

    /// <summary>
    /// Gets playback state if available after the operation.
    /// </summary>
    public PlaybackStateSnapshot? PlaybackState { get; init; }

    /// <summary>
    /// Gets replay metadata if available after the operation.
    /// </summary>
    public ReplayMetadataSnapshot? Metadata { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ReplayOperationResult Ok() => new() { Success = true };

    /// <summary>
    /// Creates a successful result with a path.
    /// </summary>
    public static ReplayOperationResult Ok(string path) =>
        new() { Success = true, Path = path };

    /// <summary>
    /// Creates a successful result with recording info.
    /// </summary>
    public static ReplayOperationResult Ok(RecordingInfoSnapshot info) =>
        new() { Success = true, RecordingInfo = info };

    /// <summary>
    /// Creates a successful result with playback state.
    /// </summary>
    public static ReplayOperationResult Ok(PlaybackStateSnapshot state) =>
        new() { Success = true, PlaybackState = state };

    /// <summary>
    /// Creates a successful result with metadata.
    /// </summary>
    public static ReplayOperationResult Ok(ReplayMetadataSnapshot metadata) =>
        new() { Success = true, Metadata = metadata };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ReplayOperationResult Fail(string error) =>
        new() { Success = false, Error = error };
}
