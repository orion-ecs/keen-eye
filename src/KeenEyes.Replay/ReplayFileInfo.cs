using System.IO.Compression;

namespace KeenEyes.Replay;

/// <summary>
/// Metadata about a replay file.
/// </summary>
/// <remarks>
/// <para>
/// This record stores information about a replay recording that is serialized
/// as part of the .kreplay file header. It allows reading metadata without
/// loading the full replay data.
/// </para>
/// </remarks>
public sealed record ReplayFileInfo
{
    /// <summary>
    /// Gets or sets the optional name of the recording.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets when the recording started.
    /// </summary>
    public DateTimeOffset RecordingStarted { get; init; }

    /// <summary>
    /// Gets or sets when the recording ended.
    /// </summary>
    public DateTimeOffset? RecordingEnded { get; init; }

    /// <summary>
    /// Gets or sets the total duration of the recording.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the number of frames in the recording.
    /// </summary>
    public int FrameCount { get; init; }

    /// <summary>
    /// Gets or sets the number of snapshots in the recording.
    /// </summary>
    public int SnapshotCount { get; init; }

    /// <summary>
    /// Gets or sets the uncompressed size of the replay data in bytes.
    /// </summary>
    public long UncompressedSize { get; init; }

    /// <summary>
    /// Gets or sets the compressed size of the replay data in bytes.
    /// </summary>
    public long CompressedSize { get; init; }

    /// <summary>
    /// Gets or sets the compression mode used.
    /// </summary>
    public CompressionMode Compression { get; init; }

    /// <summary>
    /// Gets or sets the CRC32 checksum of the compressed data (hex string).
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets or sets any validation error encountered when reading.
    /// </summary>
    public string? ValidationError { get; init; }

    /// <summary>
    /// Gets the replay data format version.
    /// </summary>
    public int DataVersion { get; init; } = ReplayData.CurrentVersion;

    /// <summary>
    /// Gets a value indicating whether this file info represents a valid file.
    /// </summary>
    public bool IsValid => ValidationError is null;

    /// <summary>
    /// Gets the compression ratio (compressed/uncompressed).
    /// </summary>
    public double CompressionRatio =>
        UncompressedSize > 0 ? (double)CompressedSize / UncompressedSize : 1.0;
}

/// <summary>
/// Compression modes for replay files.
/// </summary>
public enum CompressionMode
{
    /// <summary>
    /// No compression.
    /// </summary>
    None = 0,

    /// <summary>
    /// GZip compression.
    /// </summary>
    GZip = 1,

    /// <summary>
    /// Brotli compression.
    /// </summary>
    Brotli = 2
}
