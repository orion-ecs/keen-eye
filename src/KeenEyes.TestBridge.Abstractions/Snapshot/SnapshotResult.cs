namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Result of a snapshot operation.
/// </summary>
/// <remarks>
/// Contains success/failure status, optional error messages, and snapshot metadata
/// when the operation creates or accesses a snapshot.
/// </remarks>
public sealed record SnapshotResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the affected snapshot, if applicable.
    /// </summary>
    public string? SnapshotName { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets metadata about the snapshot, if applicable.
    /// </summary>
    public SnapshotInfo? Info { get; init; }

    /// <summary>
    /// Creates a successful result with snapshot info.
    /// </summary>
    /// <param name="name">The snapshot name.</param>
    /// <param name="info">The snapshot metadata.</param>
    /// <returns>A successful result.</returns>
    public static SnapshotResult Ok(string name, SnapshotInfo? info = null) => new()
    {
        Success = true,
        SnapshotName = name,
        Info = info
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SnapshotResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}
