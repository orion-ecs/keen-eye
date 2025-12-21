namespace KeenEyes.Assets;

/// <summary>
/// Priority level for asset loading operations.
/// </summary>
/// <remarks>
/// Lower values indicate higher priority. Assets with higher priority
/// are loaded before those with lower priority.
/// </remarks>
public enum LoadPriority
{
    /// <summary>
    /// Immediate loading priority. Use sparingly as this may cause frame hitches.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// High priority loading. For assets needed very soon.
    /// </summary>
    High = 1,

    /// <summary>
    /// Normal priority loading. Default for most assets.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Low priority loading. For assets that can wait.
    /// </summary>
    Low = 3,

    /// <summary>
    /// Background streaming priority. Lowest priority for level streaming.
    /// </summary>
    Streaming = 4
}
