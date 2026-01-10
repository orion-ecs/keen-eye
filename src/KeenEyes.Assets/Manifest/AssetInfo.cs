namespace KeenEyes.Assets;

/// <summary>
/// Information about a single asset in the manifest.
/// </summary>
/// <param name="Path">The relative path to the asset.</param>
/// <param name="Type">The asset type (e.g., "texture", "audio", "atlas").</param>
/// <param name="Size">The file size in bytes.</param>
/// <param name="Hash">Optional SHA256 hash for verification.</param>
/// <param name="Dependencies">Optional list of assets this asset depends on.</param>
public readonly record struct AssetInfo(
    string Path,
    string Type,
    long Size,
    string? Hash,
    IReadOnlyList<string>? Dependencies);
