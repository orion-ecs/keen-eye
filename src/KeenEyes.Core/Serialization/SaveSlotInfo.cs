using System.Text.Json.Serialization;

namespace KeenEyes.Serialization;

/// <summary>
/// Represents metadata about a save slot, including display information and save state.
/// </summary>
/// <remarks>
/// <para>
/// SaveSlotInfo contains all the information needed to display save slots in a UI
/// without loading the full world snapshot. This includes timestamps, display names,
/// play time, thumbnails, and custom metadata.
/// </para>
/// <para>
/// Save slot info is stored separately from the world data in the .ksave container,
/// allowing quick enumeration of available saves without parsing the full snapshot.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create save slot info when saving
/// var slotInfo = new SaveSlotInfo
/// {
///     SlotName = "slot1",
///     DisplayName = "Chapter 3 - The Forest",
///     CreatedAt = DateTimeOffset.UtcNow,
///     ModifiedAt = DateTimeOffset.UtcNow,
///     PlayTime = TimeSpan.FromHours(2.5),
///     CustomMetadata = new Dictionary&lt;string, object&gt;
///     {
///         ["level"] = 15,
///         ["location"] = "Dark Forest"
///     }
/// };
/// </code>
/// </example>
public sealed record SaveSlotInfo
{
    /// <summary>
    /// Gets the unique identifier for this save slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The slot name is used as the filename (without extension) and must be
    /// a valid filename on all target platforms. Recommended to use lowercase
    /// alphanumeric characters, numbers, and underscores only.
    /// </para>
    /// <para>
    /// Examples: "slot1", "autosave", "quicksave", "chapter3_checkpoint"
    /// </para>
    /// </remarks>
    public required string SlotName { get; init; }

    /// <summary>
    /// Gets the human-readable display name for this save slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the name shown to players in save/load UI. Can contain
    /// any characters including spaces, punctuation, and Unicode.
    /// </para>
    /// <para>
    /// If null, the <see cref="SlotName"/> should be used for display.
    /// </para>
    /// </remarks>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the timestamp when this save slot was first created.
    /// </summary>
    /// <remarks>
    /// Stored as UTC time for consistency across time zones.
    /// </remarks>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when this save slot was last modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Updated each time the save is overwritten. For a new save,
    /// this equals <see cref="CreatedAt"/>.
    /// </para>
    /// <para>
    /// Stored as UTC time for consistency across time zones.
    /// </para>
    /// </remarks>
    public required DateTimeOffset ModifiedAt { get; init; }

    /// <summary>
    /// Gets the total play time accumulated in this save.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is typically tracked by the application and passed when saving.
    /// It represents the total time spent playing in this save file.
    /// </para>
    /// <para>
    /// A value of <see cref="TimeSpan.Zero"/> indicates play time is not tracked.
    /// </para>
    /// </remarks>
    public TimeSpan PlayTime { get; init; }

    /// <summary>
    /// Gets the number of times this slot has been saved to.
    /// </summary>
    /// <remarks>
    /// Incremented each time the save is overwritten. Starts at 1.
    /// </remarks>
    public int SaveCount { get; init; } = 1;

    /// <summary>
    /// Gets the format used to serialize the world snapshot.
    /// </summary>
    public SaveFormat Format { get; init; } = SaveFormat.Binary;

    /// <summary>
    /// Gets the compression mode used for the save data.
    /// </summary>
    public CompressionMode Compression { get; init; } = CompressionMode.GZip;

    /// <summary>
    /// Gets the size of the compressed save data in bytes.
    /// </summary>
    /// <remarks>
    /// This is the actual file size after compression. Useful for displaying
    /// save slot sizes in UI and estimating storage requirements.
    /// </remarks>
    public long CompressedSize { get; init; }

    /// <summary>
    /// Gets the size of the uncompressed save data in bytes.
    /// </summary>
    /// <remarks>
    /// This is the size before compression. The ratio of
    /// <see cref="CompressedSize"/> to <see cref="UncompressedSize"/>
    /// indicates the compression efficiency.
    /// </remarks>
    public long UncompressedSize { get; init; }

    /// <summary>
    /// Gets the SHA256 checksum of the save data for integrity validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The checksum is computed over the compressed data bytes and stored
    /// as a lowercase hexadecimal string (64 characters).
    /// </para>
    /// <para>
    /// Used to detect save file corruption. If null, no checksum was computed.
    /// </para>
    /// </remarks>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the number of entities in the saved world.
    /// </summary>
    /// <remarks>
    /// Extracted from the snapshot for quick display without full deserialization.
    /// </remarks>
    public int EntityCount { get; init; }

    /// <summary>
    /// Gets the thumbnail image data as a base64-encoded string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thumbnails are typically PNG or JPEG images captured at save time.
    /// The image data is base64-encoded for JSON compatibility.
    /// </para>
    /// <para>
    /// Recommended dimensions: 320x180 (16:9) or 256x256 (1:1).
    /// Maximum recommended size: 64KB after encoding.
    /// </para>
    /// <para>
    /// If null, no thumbnail is available.
    /// </para>
    /// </remarks>
    public string? ThumbnailBase64 { get; init; }

    /// <summary>
    /// Gets the MIME type of the thumbnail image.
    /// </summary>
    /// <remarks>
    /// Common values: "image/png", "image/jpeg".
    /// If null, the MIME type should be inferred from the image data.
    /// </remarks>
    public string? ThumbnailMimeType { get; init; }

    /// <summary>
    /// Gets custom application-specific metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to store game-specific information such as:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Player level, health, score</description></item>
    /// <item><description>Current location or chapter</description></item>
    /// <item><description>Difficulty setting</description></item>
    /// <item><description>Completion percentage</description></item>
    /// </list>
    /// <para>
    /// Values must be JSON-serializable types (strings, numbers, booleans,
    /// or nested dictionaries/arrays of these types).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? CustomMetadata { get; init; }

    /// <summary>
    /// Gets or sets the version of the application that created this save.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Useful for detecting saves from older versions and applying migrations.
    /// Format is application-defined (e.g., "1.0.0", "2024.1", "build-1234").
    /// </para>
    /// </remarks>
    public string? AppVersion { get; init; }

    /// <summary>
    /// Gets the version of the save file format.
    /// </summary>
    /// <remarks>
    /// Used for backwards compatibility when the save format changes.
    /// Current version is 1.
    /// </remarks>
    public int FormatVersion { get; init; } = 1;

    /// <summary>
    /// Gets whether this save file is currently valid and loadable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A save may be invalid if:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The file is corrupted (checksum mismatch)</description></item>
    /// <item><description>The format version is unsupported</description></item>
    /// <item><description>Required components are missing</description></item>
    /// </list>
    /// </remarks>
    [JsonIgnore]
    public bool IsValid => ValidationError is null;

    /// <summary>
    /// Gets the validation error message if the save is invalid.
    /// </summary>
    /// <remarks>
    /// Null if the save is valid. Contains a human-readable error message
    /// if validation failed.
    /// </remarks>
    public string? ValidationError { get; init; }
}
