namespace KeenEyes.Assets;

/// <summary>
/// Configuration options for the asset management system.
/// </summary>
/// <remarks>
/// Use this to configure cache behavior, async loading, and development features
/// when creating an <see cref="AssetManager"/> or <see cref="AssetsPlugin"/>.
/// </remarks>
/// <example>
/// <code>
/// var config = new AssetsConfig
/// {
///     RootPath = "Assets",
///     MaxCacheBytes = 1024 * 1024 * 1024, // 1 GB
///     CachePolicy = CachePolicy.LRU,
///     EnableHotReload = true
/// };
///
/// world.InstallPlugin(new AssetsPlugin(config));
/// </code>
/// </example>
public sealed record AssetsConfig
{
    /// <summary>
    /// Gets or sets the root path for asset files.
    /// </summary>
    /// <remarks>
    /// All asset paths are relative to this root. Defaults to "Assets".
    /// </remarks>
    public string RootPath { get; init; } = "Assets";

    /// <summary>
    /// Gets or sets the maximum cache size in bytes.
    /// </summary>
    /// <remarks>
    /// When the cache exceeds this size, assets may be evicted based on
    /// <see cref="CachePolicy"/>. Defaults to 512 MB.
    /// </remarks>
    public long MaxCacheBytes { get; init; } = 512 * 1024 * 1024; // 512 MB

    /// <summary>
    /// Gets or sets the cache eviction policy.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><see cref="Assets.CachePolicy.LRU"/> - Least recently used eviction (default)</item>
    /// <item><see cref="Assets.CachePolicy.Manual"/> - Never auto-evict</item>
    /// <item><see cref="Assets.CachePolicy.Aggressive"/> - Evict immediately when refcount hits 0</item>
    /// </list>
    /// </remarks>
    public CachePolicy CachePolicy { get; init; } = CachePolicy.LRU;

    /// <summary>
    /// Gets or sets the maximum number of concurrent async loads.
    /// </summary>
    /// <remarks>
    /// Higher values allow more parallel loading but use more system resources.
    /// Defaults to 4.
    /// </remarks>
    public int MaxConcurrentLoads { get; init; } = 4;

    /// <summary>
    /// Gets or sets whether hot reload is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the asset manager watches for file changes and automatically
    /// reloads modified assets. Only enable in development. Defaults to false.
    /// </remarks>
    public bool EnableHotReload { get; init; } = false;

    /// <summary>
    /// Gets or sets the default load priority for async operations.
    /// </summary>
    public LoadPriority DefaultPriority { get; init; } = LoadPriority.Normal;

    /// <summary>
    /// Gets or sets an optional service provider for dependency injection in loaders.
    /// </summary>
    public IServiceProvider? Services { get; init; }

    /// <summary>
    /// Gets or sets an optional callback for load errors.
    /// </summary>
    /// <remarks>
    /// When set, this callback is invoked when an async load fails in the
    /// <see cref="AssetResolutionSystem"/>. Use this for logging or diagnostics.
    /// The callback receives the asset path and exception.
    /// </remarks>
    public Action<string, Exception>? OnLoadError { get; init; }

    /// <summary>
    /// Gets a default configuration suitable for production.
    /// </summary>
    public static AssetsConfig Default => new();

    /// <summary>
    /// Gets a configuration suitable for development with hot reload enabled.
    /// </summary>
    public static AssetsConfig Development => new()
    {
        EnableHotReload = true,
        CachePolicy = CachePolicy.Aggressive
    };
}
