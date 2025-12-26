using System.IO.Compression;

namespace KeenEyes.Replay;

/// <summary>
/// Options for saving replay files.
/// </summary>
/// <remarks>
/// <para>
/// These options control compression and checksum behavior when writing
/// .kreplay files. Default settings provide a good balance of file size
/// and load time.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Save with maximum compression for archival
/// var options = new ReplayFileOptions
/// {
///     Compression = CompressionMode.Brotli,
///     CompressionLevel = CompressionLevel.SmallestSize
/// };
/// ReplayFileFormat.WriteToFile("replay.kreplay", replayData, options);
///
/// // Save without compression for debugging
/// var debugOptions = new ReplayFileOptions
/// {
///     Compression = CompressionMode.None,
///     IncludeChecksum = false
/// };
/// </code>
/// </example>
public sealed record ReplayFileOptions
{
    /// <summary>
    /// Gets the default options (GZip compression, checksum enabled).
    /// </summary>
    public static ReplayFileOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the compression mode to use.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="CompressionMode.GZip"/> which provides a good
    /// balance of compression ratio and speed.
    /// </remarks>
    public CompressionMode Compression { get; init; } = CompressionMode.GZip;

    /// <summary>
    /// Gets or sets the compression level.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// Use <see cref="System.IO.Compression.CompressionLevel.Fastest"/> for faster saves
    /// or <see cref="System.IO.Compression.CompressionLevel.SmallestSize"/> for smallest files.
    /// </remarks>
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets whether to include a CRC32 checksum.
    /// </summary>
    /// <remarks>
    /// Default is true. The checksum enables detection of file corruption
    /// during loading.
    /// </remarks>
    public bool IncludeChecksum { get; init; } = true;
}
