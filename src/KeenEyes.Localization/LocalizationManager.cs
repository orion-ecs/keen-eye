using System.Diagnostics.CodeAnalysis;

namespace KeenEyes.Localization;

/// <summary>
/// Manages localization state and string resolution for a world.
/// </summary>
/// <remarks>
/// <para>
/// This class is marked with <see cref="PluginExtensionAttribute"/> to generate
/// a <c>world.Localization</c> accessor property.
/// </para>
/// </remarks>
[PluginExtension("Localization")]
public sealed class LocalizationManager : ILocalization, IDisposable
{
    private readonly LocalizationConfig config;
    private readonly IWorld world;
    private readonly List<IStringSource> sources = [];
    private readonly Lock sourcesLock = new();
    private readonly IMessageFormatter icuFormatter;
    private Locale currentLocale;
    private bool disposed;

    /// <summary>
    /// Creates a new localization manager with the specified configuration.
    /// </summary>
    /// <param name="config">The localization configuration.</param>
    /// <param name="world">The world for publishing locale change events.</param>
    internal LocalizationManager(LocalizationConfig config, IWorld world)
        : this(config, world, IcuFormatter.Instance)
    {
    }

    /// <summary>
    /// Creates a new localization manager with the specified configuration and formatter.
    /// </summary>
    /// <param name="config">The localization configuration.</param>
    /// <param name="world">The world for publishing locale change events.</param>
    /// <param name="icuFormatter">The formatter to use for ICU MessageFormat strings.</param>
    internal LocalizationManager(LocalizationConfig config, IWorld world, IMessageFormatter icuFormatter)
    {
        this.config = config;
        this.world = world;
        this.icuFormatter = icuFormatter;
        currentLocale = config.DefaultLocale;
    }

    /// <inheritdoc />
    public Locale CurrentLocale => currentLocale;

    /// <inheritdoc />
    public IEnumerable<Locale> AvailableLocales
    {
        get
        {
            var locales = new HashSet<Locale>();
            lock (sourcesLock)
            {
                foreach (var source in sources)
                {
                    foreach (var locale in source.SupportedLocales)
                    {
                        locales.Add(locale);
                    }
                }
            }
            return locales;
        }
    }

    /// <inheritdoc />
    public string Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (TryGetWithFallback(key, out var value))
        {
            return value!;
        }

        return HandleMissingKey(key);
    }

    /// <inheritdoc />
    public string Format(string key, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(key);

        var template = Get(key);
        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // If formatting fails, return the template with the key info
            return template;
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object> instead.")]
    public string FormatIcu(string key, object? args)
    {
        ArgumentNullException.ThrowIfNull(key);

        var template = Get(key);
        return icuFormatter.Format(template, args, currentLocale);
    }

    /// <inheritdoc />
    public void SetLocale(Locale locale)
    {
        if (currentLocale == locale)
        {
            return;
        }

        var previous = currentLocale;
        currentLocale = locale;

        world.Send(new LocaleChangedEvent(previous, locale));
    }

    /// <inheritdoc />
    public bool TryGet(string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return TryGetForLocale(currentLocale, key, out value);
    }

    /// <inheritdoc />
    public void AddSource(IStringSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (sourcesLock)
        {
            sources.Add(source);
        }
    }

    /// <inheritdoc />
    public bool RemoveSource(IStringSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (sourcesLock)
        {
            return sources.Remove(source);
        }
    }

    /// <inheritdoc />
    public bool HasKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return TryGetWithFallback(key, out _);
    }

    /// <inheritdoc />
    public LocalizedFontConfig? GetFontConfig(Locale locale)
    {
        // Try exact locale match
        if (config.FontConfigs.TryGetValue(locale, out var fontConfig))
        {
            return fontConfig;
        }

        // Try language-only fallback
        if (locale.HasRegion)
        {
            if (config.FontConfigs.TryGetValue(locale.LanguageOnly, out fontConfig))
            {
                return fontConfig;
            }
        }

        // Try default locale's configuration
        if (locale != config.DefaultLocale)
        {
            if (config.FontConfigs.TryGetValue(config.DefaultLocale, out fontConfig))
            {
                return fontConfig;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public LocalizedFontConfig? GetCurrentFontConfig()
    {
        return GetFontConfig(currentLocale);
    }

    /// <inheritdoc />
    public async Task PreloadLocaleAssetsAsync(
        Locale locale,
        IEnumerable<string> assetKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assetKeys);

        // Get the asset resolver from the world
        if (!world.TryGetExtension<ILocalizedAssetResolver>(out var resolver) || resolver == null)
        {
            // No resolver available, nothing to preload
            return;
        }

        // Resolve all asset paths for the target locale
        var pathsToLoad = new List<string>();
        foreach (var assetKey in assetKeys)
        {
            var resolvedPath = resolver.Resolve(assetKey, locale);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                pathsToLoad.Add(resolvedPath);
            }
        }

        // Also preload font assets for this locale
        var fontConfig = GetFontConfig(locale);
        if (fontConfig != null)
        {
            foreach (var fontPath in fontConfig.GetAllFontPaths())
            {
                pathsToLoad.Add(fontPath);
            }
        }

        // Try to use the asset manager to preload
        if (!world.TryGetExtension<KeenEyes.Assets.AssetManager>(out var assetManager) || assetManager == null)
        {
            // No asset manager available, nothing more we can do
            return;
        }

        // Preload all assets in parallel as RawAsset for caching
        var loadTasks = new List<Task>();
        foreach (var path in pathsToLoad.Distinct())
        {
            if (!assetManager.IsLoaded(path))
            {
                loadTasks.Add(LoadAssetForPreloadAsync(assetManager, path, cancellationToken));
            }
        }

        await Task.WhenAll(loadTasks);
    }

    private static async Task LoadAssetForPreloadAsync(
        KeenEyes.Assets.AssetManager assetManager,
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            // Preload as RawAsset - this caches the file contents
            // The actual typed load will happen when the asset is used
            await assetManager.LoadAsync<KeenEyes.Assets.RawAsset>(
                path,
                KeenEyes.Assets.LoadPriority.Low,
                cancellationToken);
        }
        catch
        {
            // Silently ignore preload failures - the asset might not exist
            // or might need a specialized loader. The actual load will report errors.
        }
    }

    /// <summary>
    /// Clears all registered string sources.
    /// </summary>
    public void Clear()
    {
        lock (sourcesLock)
        {
            sources.Clear();
        }
    }

    /// <summary>
    /// Gets the number of registered string sources.
    /// </summary>
    public int SourceCount
    {
        get
        {
            lock (sourcesLock)
            {
                return sources.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Clear();
    }

    private bool TryGetWithFallback(string key, out string? value)
    {
        // Try current locale first
        if (TryGetForLocale(currentLocale, key, out value))
        {
            return true;
        }

        if (!config.EnableFallback)
        {
            value = null;
            return false;
        }

        // Try custom fallback override
        if (config.FallbackOverrides.TryGetValue(currentLocale, out var fallbackLocale))
        {
            if (TryGetForLocale(fallbackLocale, key, out value))
            {
                return true;
            }
        }

        // Try language-only fallback (e.g., "en-US" -> "en")
        if (currentLocale.HasRegion)
        {
            var languageOnly = currentLocale.LanguageOnly;
            if (TryGetForLocale(languageOnly, key, out value))
            {
                return true;
            }
        }

        // Try default locale as last resort
        if (currentLocale != config.DefaultLocale)
        {
            if (TryGetForLocale(config.DefaultLocale, key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private bool TryGetForLocale(Locale locale, string key, out string? value)
    {
        lock (sourcesLock)
        {
            // Search sources in reverse order (later sources override earlier)
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i].TryGetString(locale, key, out value))
                {
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private string HandleMissingKey(string key)
    {
        return config.MissingKeyBehavior switch
        {
            MissingKeyBehavior.ReturnKey => key,
            MissingKeyBehavior.ReturnEmpty => string.Empty,
            MissingKeyBehavior.ReturnPlaceholder => $"[MISSING: {key}]",
            MissingKeyBehavior.ThrowException => throw new KeyNotFoundException(
                $"Localization key not found: '{key}' for locale '{currentLocale}'"),
            _ => key
        };
    }
}
