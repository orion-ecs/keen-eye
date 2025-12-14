using System.IO.Compression;

namespace KeenEyes.Serialization;

/// <summary>
/// Options for configuring save operations.
/// </summary>
/// <remarks>
/// <para>
/// Use this to customize how saves are created, including compression,
/// checksums, and metadata. Default values are optimized for most use cases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SaveSlotOptions
/// {
///     Compression = CompressionMode.Brotli,
///     CompressionLevel = CompressionLevel.SmallestSize,
///     IncludeChecksum = true,
///     DisplayName = "Autosave - Level 15"
/// };
/// </code>
/// </example>
public sealed record SaveSlotOptions
{
    /// <summary>
    /// Gets or sets the serialization format.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SaveFormat.Binary"/> for optimal performance and size.
    /// </remarks>
    public SaveFormat Format { get; init; } = SaveFormat.Binary;

    /// <summary>
    /// Gets or sets the compression mode.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CompressionMode.GZip"/> for a good balance of
    /// speed and compression ratio.
    /// </remarks>
    public CompressionMode Compression { get; init; } = CompressionMode.GZip;

    /// <summary>
    /// Gets or sets the compression level when compression is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Higher compression levels result in smaller files but take longer to compress.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="CompressionLevel.Fastest"/> - Minimal compression, fastest speed</description></item>
    /// <item><description><see cref="CompressionLevel.Optimal"/> - Good balance (default)</description></item>
    /// <item><description><see cref="CompressionLevel.SmallestSize"/> - Best compression, slowest speed</description></item>
    /// </list>
    /// </remarks>
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets whether to compute and store a SHA256 checksum.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, a SHA256 hash is computed over the compressed data and stored
    /// in the save file. This allows detection of file corruption during load.
    /// </para>
    /// <para>
    /// Defaults to true. Disable only if performance is critical and corruption
    /// detection is handled externally.
    /// </para>
    /// </remarks>
    public bool IncludeChecksum { get; init; } = true;

    /// <summary>
    /// Gets or sets the human-readable display name for the save slot.
    /// </summary>
    /// <remarks>
    /// If null, the slot name will be used for display.
    /// </remarks>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or sets custom application-specific metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Values must be JSON-serializable types (strings, numbers, booleans,
    /// or nested dictionaries/arrays of these types).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? CustomMetadata { get; init; }

    /// <summary>
    /// Gets or sets the accumulated play time to store with this save.
    /// </summary>
    public TimeSpan PlayTime { get; init; }

    /// <summary>
    /// Gets or sets the thumbnail image data as bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Should be PNG or JPEG image data. Will be base64-encoded when stored.
    /// </para>
    /// <para>
    /// Recommended dimensions: 320x180 (16:9) or 256x256 (1:1).
    /// Maximum recommended size: 48KB before encoding.
    /// </para>
    /// </remarks>
    public byte[]? ThumbnailData { get; init; }

    /// <summary>
    /// Gets or sets the MIME type of the thumbnail image.
    /// </summary>
    /// <remarks>
    /// Common values: "image/png", "image/jpeg".
    /// Required if <see cref="ThumbnailData"/> is provided.
    /// </remarks>
    public string? ThumbnailMimeType { get; init; }

    /// <summary>
    /// Gets or sets the application version to store with this save.
    /// </summary>
    /// <remarks>
    /// Format is application-defined (e.g., "1.0.0", "2024.1", "build-1234").
    /// </remarks>
    public string? AppVersion { get; init; }

    /// <summary>
    /// Gets default options optimized for typical use cases.
    /// </summary>
    /// <remarks>
    /// Uses binary format with GZip compression and SHA256 checksums.
    /// </remarks>
    public static SaveSlotOptions Default { get; } = new();

    /// <summary>
    /// Gets options optimized for fastest save/load performance.
    /// </summary>
    /// <remarks>
    /// Uses binary format with fastest compression and no checksums.
    /// </remarks>
    public static SaveSlotOptions Fast { get; } = new()
    {
        CompressionLevel = CompressionLevel.Fastest,
        IncludeChecksum = false
    };

    /// <summary>
    /// Gets options optimized for smallest file size.
    /// </summary>
    /// <remarks>
    /// Uses binary format with Brotli compression at maximum level.
    /// </remarks>
    public static SaveSlotOptions Compact { get; } = new()
    {
        Compression = CompressionMode.Brotli,
        CompressionLevel = CompressionLevel.SmallestSize,
        IncludeChecksum = true
    };

    /// <summary>
    /// Gets options for human-readable save files (debugging).
    /// </summary>
    /// <remarks>
    /// Uses JSON format with no compression. Files are larger but can be
    /// manually inspected and edited.
    /// </remarks>
    public static SaveSlotOptions Debug { get; } = new()
    {
        Format = SaveFormat.Json,
        Compression = CompressionMode.None,
        IncludeChecksum = false
    };
}
