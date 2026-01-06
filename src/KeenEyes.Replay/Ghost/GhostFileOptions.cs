using System.IO.Compression;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Options for writing ghost files.
/// </summary>
/// <remarks>
/// <para>
/// These options control how ghost data is written to files, including
/// compression settings and checksum generation.
/// </para>
/// <para>
/// Ghost files are typically small (KB range), so compression may not
/// always be beneficial. For very small ghosts, uncompressed files may
/// be preferred for faster loading.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use Brotli compression with maximum efficiency
/// var options = new GhostFileOptions
/// {
///     Compression = CompressionMode.Brotli,
///     CompressionLevel = CompressionLevel.SmallestSize,
///     IncludeChecksum = true
/// };
///
/// GhostFileFormat.WriteToFile("ghost.keghost", ghostData, options);
/// </code>
/// </example>
public sealed record GhostFileOptions
{
    /// <summary>
    /// Gets the default file options.
    /// </summary>
    /// <remarks>
    /// Default settings: GZip compression, optimal level, checksum enabled.
    /// </remarks>
    public static GhostFileOptions Default { get; } = new();

    /// <summary>
    /// Gets file options optimized for smallest file size.
    /// </summary>
    /// <remarks>
    /// Uses Brotli compression at SmallestSize level with checksum.
    /// Best for distribution or long-term storage.
    /// </remarks>
    public static GhostFileOptions Smallest { get; } = new()
    {
        Compression = CompressionMode.Brotli,
        CompressionLevel = CompressionLevel.SmallestSize,
        IncludeChecksum = true
    };

    /// <summary>
    /// Gets file options optimized for fastest save/load.
    /// </summary>
    /// <remarks>
    /// Uses no compression for fastest possible I/O.
    /// Best for temporary files or development.
    /// </remarks>
    public static GhostFileOptions Fastest { get; } = new()
    {
        Compression = CompressionMode.None,
        CompressionLevel = CompressionLevel.Fastest,
        IncludeChecksum = false
    };

    /// <summary>
    /// Gets or sets the compression mode to use.
    /// </summary>
    /// <value>Default is <see cref="CompressionMode.GZip"/>.</value>
    public CompressionMode Compression { get; init; } = CompressionMode.GZip;

    /// <summary>
    /// Gets or sets the compression level to use.
    /// </summary>
    /// <value>Default is <see cref="CompressionLevel.Optimal"/>.</value>
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets whether to include a CRC32 checksum in the file.
    /// </summary>
    /// <value>Default is true.</value>
    /// <remarks>
    /// The checksum enables corruption detection when loading ghost files.
    /// Disabling it slightly reduces file size and save time.
    /// </remarks>
    public bool IncludeChecksum { get; init; } = true;
}
