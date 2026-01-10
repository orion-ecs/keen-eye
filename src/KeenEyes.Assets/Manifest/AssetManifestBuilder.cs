namespace KeenEyes.Assets;

/// <summary>
/// Builder for creating asset manifests programmatically.
/// </summary>
/// <remarks>
/// Use this builder to construct manifests at build time or for testing.
/// </remarks>
public sealed class AssetManifestBuilder
{
    private readonly List<AssetInfo> assets = [];
    private int version = 1;
    private DateTime? generated;

    /// <summary>
    /// Sets the manifest version.
    /// </summary>
    /// <param name="version">The version number.</param>
    /// <returns>This builder for chaining.</returns>
    public AssetManifestBuilder WithVersion(int version)
    {
        this.version = version;
        return this;
    }

    /// <summary>
    /// Sets the generation timestamp.
    /// </summary>
    /// <param name="timestamp">The generation time.</param>
    /// <returns>This builder for chaining.</returns>
    public AssetManifestBuilder WithGeneratedTime(DateTime timestamp)
    {
        generated = timestamp;
        return this;
    }

    /// <summary>
    /// Adds an asset to the manifest.
    /// </summary>
    /// <param name="path">The relative path to the asset.</param>
    /// <param name="type">The asset type.</param>
    /// <param name="size">The file size in bytes.</param>
    /// <param name="hash">Optional SHA256 hash.</param>
    /// <param name="dependencies">Optional list of dependencies.</param>
    /// <returns>This builder for chaining.</returns>
    public AssetManifestBuilder AddAsset(
        string path,
        string type,
        long size,
        string? hash = null,
        IReadOnlyList<string>? dependencies = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(type);

        assets.Add(new AssetInfo(path, type, size, hash, dependencies));
        return this;
    }

    /// <summary>
    /// Adds an asset to the manifest.
    /// </summary>
    /// <param name="asset">The asset info to add.</param>
    /// <returns>This builder for chaining.</returns>
    public AssetManifestBuilder AddAsset(AssetInfo asset)
    {
        assets.Add(asset);
        return this;
    }

    /// <summary>
    /// Adds multiple assets to the manifest.
    /// </summary>
    /// <param name="assetInfos">The assets to add.</param>
    /// <returns>This builder for chaining.</returns>
    public AssetManifestBuilder AddAssets(IEnumerable<AssetInfo> assetInfos)
    {
        ArgumentNullException.ThrowIfNull(assetInfos);

        assets.AddRange(assetInfos);
        return this;
    }

    /// <summary>
    /// Builds the asset manifest.
    /// </summary>
    /// <returns>The constructed manifest.</returns>
    public AssetManifest Build()
    {
        return new AssetManifest(
            version,
            generated ?? DateTime.UtcNow,
            assets.ToList());
    }
}
