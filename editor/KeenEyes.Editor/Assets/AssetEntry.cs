namespace KeenEyes.Editor.Assets;

/// <summary>
/// Represents an asset entry in the asset database.
/// </summary>
public sealed class AssetEntry
{
    /// <summary>
    /// Gets or sets the asset name (filename without extension).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the relative path from project root.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets or sets the full absolute path.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Gets or sets the asset type.
    /// </summary>
    public required AssetType Type { get; init; }

    /// <summary>
    /// Gets or sets when the asset was last modified.
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Type})";
}
