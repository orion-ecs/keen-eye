namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Contains metadata about a ghost file without loading the full data.
/// </summary>
/// <remarks>
/// <para>
/// This record provides quick access to ghost file information for purposes
/// such as listing available ghosts, displaying file details, or validating
/// files before loading.
/// </para>
/// <para>
/// Use <see cref="GhostFileFormat.ReadMetadata(Stream)"/> to read just the
/// metadata without decompressing the full ghost data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Read metadata without loading full ghost
/// var info = GhostFileFormat.ReadMetadataFromFile("personal_best.keghost");
///
/// Console.WriteLine($"Ghost: {info.Name}");
/// Console.WriteLine($"Duration: {info.Duration}");
/// Console.WriteLine($"Frames: {info.FrameCount}");
/// Console.WriteLine($"Size: {info.CompressedSize / 1024.0:F1} KB");
/// </code>
/// </example>
public sealed record GhostFileInfo
{
    /// <summary>
    /// Gets or sets the name of the ghost recording.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the name of the entity this ghost represents.
    /// </summary>
    public string? EntityName { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the original recording started.
    /// </summary>
    public DateTimeOffset RecordingStarted { get; init; }

    /// <summary>
    /// Gets or sets the total duration of the ghost recording.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the total number of frames in the ghost recording.
    /// </summary>
    public int FrameCount { get; init; }

    /// <summary>
    /// Gets or sets the total distance traveled by the ghost.
    /// </summary>
    public float TotalDistance { get; init; }

    /// <summary>
    /// Gets or sets the uncompressed size of the ghost data in bytes.
    /// </summary>
    public long UncompressedSize { get; init; }

    /// <summary>
    /// Gets or sets the compressed size of the ghost data in bytes.
    /// </summary>
    public long CompressedSize { get; init; }

    /// <summary>
    /// Gets or sets the compression mode used for the ghost data.
    /// </summary>
    public CompressionMode Compression { get; init; }

    /// <summary>
    /// Gets or sets the checksum of the compressed data, if computed.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets or sets the ghost data format version.
    /// </summary>
    public int DataVersion { get; init; } = GhostData.CurrentVersion;

    /// <summary>
    /// Gets or sets a validation error message if the file is invalid.
    /// </summary>
    /// <remarks>
    /// This property is null for valid files. When validation fails,
    /// it contains a description of the error.
    /// </remarks>
    public string? ValidationError { get; init; }

    /// <summary>
    /// Gets a value indicating whether the ghost file is valid.
    /// </summary>
    public bool IsValid => ValidationError is null;

    /// <summary>
    /// Gets the compression ratio (compressed / uncompressed).
    /// </summary>
    /// <remarks>
    /// A value less than 1.0 indicates effective compression.
    /// For example, 0.25 means the compressed size is 25% of the original.
    /// </remarks>
    public double CompressionRatio =>
        UncompressedSize > 0 ? (double)CompressedSize / UncompressedSize : 1.0;
}
