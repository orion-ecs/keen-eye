namespace KeenEyes.Localization;

/// <summary>
/// Resolves asset paths based on the current locale.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface determine how asset keys are mapped to
/// actual file paths based on the current locale, including fallback behavior.
/// </para>
/// </remarks>
public interface ILocalizedAssetResolver
{
    /// <summary>
    /// Resolves an asset key to a locale-specific path.
    /// </summary>
    /// <param name="assetKey">The base asset key (e.g., "textures/logo").</param>
    /// <param name="locale">The locale to resolve for.</param>
    /// <returns>
    /// The resolved path if a matching asset exists, or null if no asset could be found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The resolver should check for locale-specific variants in this order:
    /// </para>
    /// <list type="number">
    /// <item><description>Exact locale: <c>{key}.{locale}.{ext}</c></description></item>
    /// <item><description>Language only: <c>{key}.{language}.{ext}</c></description></item>
    /// <item><description>Default: <c>{key}.{ext}</c></description></item>
    /// </list>
    /// </remarks>
    string? Resolve(string assetKey, Locale locale);

    /// <summary>
    /// Gets all available paths for an asset key across all locales.
    /// </summary>
    /// <param name="assetKey">The base asset key.</param>
    /// <returns>An enumerable of all paths that match the asset key.</returns>
    /// <remarks>
    /// This is useful for preloading all locale variants of an asset.
    /// </remarks>
    IEnumerable<string> GetAllPaths(string assetKey);

    /// <summary>
    /// Checks if a locale-specific variant exists for the given asset key.
    /// </summary>
    /// <param name="assetKey">The base asset key.</param>
    /// <param name="locale">The locale to check.</param>
    /// <returns>True if a locale-specific variant exists, false otherwise.</returns>
    bool HasLocaleVariant(string assetKey, Locale locale);
}
