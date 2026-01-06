namespace KeenEyes.Localization;

/// <summary>
/// Component that marks an entity as having a localized asset reference.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that reference assets which should be
/// automatically swapped when the locale changes (e.g., textures with localized text,
/// audio files in different languages, region-specific content).
/// </para>
/// <para>
/// The <see cref="LocalizedAssetSystem"/> watches for locale changes and updates
/// the <see cref="ResolvedPath"/> based on the asset key and current locale.
/// </para>
/// <para>
/// Asset resolution follows a fallback chain:
/// </para>
/// <list type="number">
/// <item><description>Exact locale match: <c>textures/logo.en-US.png</c></description></item>
/// <item><description>Language only: <c>textures/logo.en.png</c></description></item>
/// <item><description>Default fallback: <c>textures/logo.png</c></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create a sprite with a localized texture
/// var logo = world.Spawn()
///     .With(SpriteRenderer.Create())
///     .With(new LocalizedAsset { AssetKey = "textures/logo" })
///     .Build();
///
/// // When locale is "en-US", ResolvedPath becomes "textures/logo.en-US.png"
/// // or falls back to "textures/logo.en.png" or "textures/logo.png"
/// </code>
/// </example>
[Component]
public partial struct LocalizedAsset : IComponent
{
    /// <summary>
    /// The base asset key without locale suffix or extension.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This key is used to find locale-specific variants of the asset.
    /// The system will search for files with locale suffixes appended.
    /// </para>
    /// <para>
    /// Example: For <c>AssetKey = "textures/logo"</c>, the system searches for:
    /// <list type="bullet">
    /// <item><c>textures/logo.en-US.png</c></item>
    /// <item><c>textures/logo.en.png</c></item>
    /// <item><c>textures/logo.png</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string AssetKey;

    /// <summary>
    /// The resolved asset path for the current locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is automatically set by the <see cref="LocalizedAssetSystem"/> based on
    /// the <see cref="AssetKey"/> and available locale-specific files.
    /// </para>
    /// <para>
    /// Use this path to load the actual asset from the asset manager.
    /// </para>
    /// </remarks>
    public string? ResolvedPath;

    /// <summary>
    /// Gets whether this asset has been resolved to a path.
    /// </summary>
    public readonly bool IsResolved => !string.IsNullOrEmpty(ResolvedPath);

    /// <summary>
    /// Creates a LocalizedAsset component with the specified asset key.
    /// </summary>
    /// <param name="assetKey">The base asset key.</param>
    public static LocalizedAsset Create(string assetKey) => new()
    {
        AssetKey = assetKey,
        ResolvedPath = null
    };

    /// <summary>
    /// Clears the resolved path, forcing re-resolution on next update.
    /// </summary>
    /// <remarks>
    /// Call this after changing the <see cref="AssetKey"/> to trigger reloading.
    /// </remarks>
    public void Invalidate()
    {
        ResolvedPath = null;
    }
}
