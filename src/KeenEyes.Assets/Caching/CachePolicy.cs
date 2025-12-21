namespace KeenEyes.Assets;

/// <summary>
/// Policy for managing asset cache eviction.
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// Least Recently Used eviction. Assets not accessed recently are
    /// evicted first when the cache exceeds size limits.
    /// </summary>
    LRU,

    /// <summary>
    /// Manual eviction only. Assets are never automatically unloaded;
    /// they must be explicitly unloaded by the application.
    /// </summary>
    Manual,

    /// <summary>
    /// Aggressive eviction. Assets are unloaded immediately when their
    /// reference count reaches zero.
    /// </summary>
    Aggressive
}
