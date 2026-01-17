namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Information about a replay file on disk.
/// </summary>
public sealed record ReplayFileSnapshot
{
    /// <summary>
    /// Gets the full file path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Gets the last modified timestamp.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Gets the replay metadata, if readable.
    /// </summary>
    public ReplayMetadataSnapshot? Metadata { get; init; }

    /// <summary>
    /// Gets any validation error for the file.
    /// </summary>
    public string? ValidationError { get; init; }
}
