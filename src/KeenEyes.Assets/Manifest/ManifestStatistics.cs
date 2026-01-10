namespace KeenEyes.Assets;

/// <summary>
/// Statistics about the assets in a manifest.
/// </summary>
/// <param name="TotalAssets">The total number of assets.</param>
/// <param name="TotalSize">The total size of all assets in bytes.</param>
/// <param name="ByType">Asset counts grouped by type.</param>
public readonly record struct ManifestStatistics(
    int TotalAssets,
    long TotalSize,
    IReadOnlyDictionary<string, int> ByType);
