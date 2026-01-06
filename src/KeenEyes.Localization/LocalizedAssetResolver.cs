namespace KeenEyes.Localization;

/// <summary>
/// Default implementation of <see cref="ILocalizedAssetResolver"/> that resolves
/// locale-specific assets using file system checks.
/// </summary>
/// <remarks>
/// <para>
/// This resolver searches for locale-specific asset variants in the following order:
/// </para>
/// <list type="number">
/// <item><description>Exact locale match: <c>textures/logo.en-US.png</c></description></item>
/// <item><description>Language only: <c>textures/logo.en.png</c></description></item>
/// <item><description>Default fallback: <c>textures/logo.png</c></description></item>
/// </list>
/// <para>
/// The resolver supports custom fallback chains via <see cref="LocalizationConfig.FallbackOverrides"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new LocalizedAssetResolver("Assets", config);
///
/// // For locale "en-US", checks in order:
/// // 1. Assets/textures/logo.en-US.png
/// // 2. Assets/textures/logo.en.png
/// // 3. Assets/textures/logo.png
/// var path = resolver.Resolve("textures/logo", Locale.EnglishUS);
/// </code>
/// </example>
/// <param name="rootPath">The root path for asset files.</param>
/// <param name="config">The localization configuration for fallback behavior.</param>
public sealed class LocalizedAssetResolver(string rootPath, LocalizationConfig config) : ILocalizedAssetResolver
{
    private readonly string rootPath = rootPath ?? string.Empty;
    private readonly LocalizationConfig config = config ?? LocalizationConfig.Default;
    private readonly string[] commonExtensions = [".png", ".jpg", ".jpeg", ".webp", ".wav", ".ogg", ".mp3", ".ttf", ".otf"];

    /// <inheritdoc />
    public string? Resolve(string assetKey, Locale locale)
    {
        if (string.IsNullOrEmpty(assetKey))
        {
            return null;
        }

        // Try exact locale match
        var path = TryResolveForLocale(assetKey, locale);
        if (path != null)
        {
            return path;
        }

        if (!config.EnableFallback)
        {
            return TryResolveDefault(assetKey);
        }

        // Try custom fallback override
        if (config.FallbackOverrides.TryGetValue(locale, out var fallbackLocale))
        {
            path = TryResolveForLocale(assetKey, fallbackLocale);
            if (path != null)
            {
                return path;
            }
        }

        // Try language-only fallback (e.g., "en-US" -> "en")
        if (locale.HasRegion)
        {
            path = TryResolveForLocale(assetKey, locale.LanguageOnly);
            if (path != null)
            {
                return path;
            }
        }

        // Try default locale as last resort
        if (locale != config.DefaultLocale)
        {
            path = TryResolveForLocale(assetKey, config.DefaultLocale);
            if (path != null)
            {
                return path;
            }

            // Try language-only of default locale
            if (config.DefaultLocale.HasRegion)
            {
                path = TryResolveForLocale(assetKey, config.DefaultLocale.LanguageOnly);
                if (path != null)
                {
                    return path;
                }
            }
        }

        // Fall back to unlocalized asset
        return TryResolveDefault(assetKey);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllPaths(string assetKey)
    {
        if (string.IsNullOrEmpty(assetKey))
        {
            yield break;
        }

        var directory = Path.GetDirectoryName(assetKey);
        var baseName = Path.GetFileNameWithoutExtension(assetKey);

        var searchDir = string.IsNullOrEmpty(rootPath)
            ? (string.IsNullOrEmpty(directory) ? "." : directory)
            : (string.IsNullOrEmpty(directory) ? rootPath : Path.Combine(rootPath, directory));

        if (!Directory.Exists(searchDir))
        {
            yield break;
        }

        // Find all files that match the base name pattern
        foreach (var file in Directory.EnumerateFiles(searchDir))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(baseName, StringComparison.OrdinalIgnoreCase))
            {
                // Return path relative to root
                var relativePath = string.IsNullOrEmpty(rootPath)
                    ? file
                    : Path.GetRelativePath(rootPath, file);
                yield return relativePath;
            }
        }
    }

    /// <inheritdoc />
    public bool HasLocaleVariant(string assetKey, Locale locale)
    {
        if (string.IsNullOrEmpty(assetKey))
        {
            return false;
        }

        return TryResolveForLocale(assetKey, locale) != null;
    }

    private string? TryResolveForLocale(string assetKey, Locale locale)
    {
        // Check if assetKey already has an extension
        var existingExt = Path.GetExtension(assetKey);
        if (!string.IsNullOrEmpty(existingExt))
        {
            // Asset key has extension, insert locale before it
            var basePath = assetKey[..^existingExt.Length];
            var localizedPath = $"{basePath}.{locale.Code}{existingExt}";
            var fullPath = string.IsNullOrEmpty(rootPath)
                ? localizedPath
                : Path.Combine(rootPath, localizedPath);

            if (File.Exists(fullPath))
            {
                return localizedPath;
            }
        }
        else
        {
            // No extension, try common extensions
            foreach (var ext in commonExtensions)
            {
                var localizedPath = $"{assetKey}.{locale.Code}{ext}";
                var fullPath = string.IsNullOrEmpty(rootPath)
                    ? localizedPath
                    : Path.Combine(rootPath, localizedPath);

                if (File.Exists(fullPath))
                {
                    return localizedPath;
                }
            }
        }

        return null;
    }

    private string? TryResolveDefault(string assetKey)
    {
        // Check if assetKey already has an extension
        var existingExt = Path.GetExtension(assetKey);
        if (!string.IsNullOrEmpty(existingExt))
        {
            var fullPath = string.IsNullOrEmpty(rootPath)
                ? assetKey
                : Path.Combine(rootPath, assetKey);

            if (File.Exists(fullPath))
            {
                return assetKey;
            }
        }
        else
        {
            // No extension, try common extensions
            foreach (var ext in commonExtensions)
            {
                var defaultPath = $"{assetKey}{ext}";
                var fullPath = string.IsNullOrEmpty(rootPath)
                    ? defaultPath
                    : Path.Combine(rootPath, defaultPath);

                if (File.Exists(fullPath))
                {
                    return defaultPath;
                }
            }
        }

        return null;
    }
}
