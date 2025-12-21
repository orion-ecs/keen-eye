namespace KeenEyes.Assets;

/// <summary>
/// Policy for managing asset cache eviction.
/// </summary>
/// <remarks>
/// <para>
/// All eviction policies respect reference counting: assets with active
/// <see cref="AssetHandle{T}"/> references are never evicted regardless of policy.
/// Only assets with zero references are candidates for eviction.
/// </para>
/// </remarks>
public enum CachePolicy
{
    /// <summary>
    /// Least Recently Used eviction (default).
    /// </summary>
    /// <remarks>
    /// Assets with zero references are evicted based on last access time when
    /// the cache exceeds size limits. Oldest unused assets are evicted first.
    /// Use <see cref="AssetManager.TrimCache"/> to trigger manual trimming, or
    /// let the cache auto-trim when size limits are exceeded.
    /// </remarks>
    LRU,

    /// <summary>
    /// Manual eviction only.
    /// </summary>
    /// <remarks>
    /// Assets are never automatically unloaded regardless of cache size.
    /// Use <see cref="AssetManager.Unload"/> or <see cref="AssetManager.UnloadAll"/>
    /// to explicitly release assets. Suitable for games with discrete levels where
    /// all assets can be unloaded at once during transitions.
    /// </remarks>
    Manual,

    /// <summary>
    /// Aggressive eviction for minimal memory usage.
    /// </summary>
    /// <remarks>
    /// Assets are unloaded immediately when their reference count reaches zero.
    /// There is no caching benefit; assets are reloaded from disk on every access.
    /// Use this for development/debugging or memory-constrained environments.
    /// </remarks>
    Aggressive
}
