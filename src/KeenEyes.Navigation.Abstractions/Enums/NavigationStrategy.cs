namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Specifies the navigation strategy used for pathfinding.
/// </summary>
/// <remarks>
/// Different strategies offer trade-offs between accuracy, memory usage,
/// and computational cost. Choose based on your game's specific needs.
/// </remarks>
public enum NavigationStrategy
{
    /// <summary>
    /// Navigation mesh-based pathfinding using connected polygons.
    /// </summary>
    /// <remarks>
    /// Best for complex 3D environments with varied terrain.
    /// Offers high accuracy and smooth paths.
    /// </remarks>
    NavMesh,

    /// <summary>
    /// Grid-based pathfinding using discrete cells.
    /// </summary>
    /// <remarks>
    /// Simpler than NavMesh, well-suited for 2D games or
    /// environments with uniform tile-based layouts.
    /// </remarks>
    Grid,

    /// <summary>
    /// Hierarchical pathfinding with multi-level abstraction.
    /// </summary>
    /// <remarks>
    /// Uses a coarse high-level path refined by local detailed paths.
    /// Excellent for large open worlds where long-distance paths are common.
    /// </remarks>
    Hierarchical,

    /// <summary>
    /// Custom user-defined navigation strategy.
    /// </summary>
    /// <remarks>
    /// Allows integration of custom pathfinding algorithms.
    /// The implementation must handle all path computation.
    /// </remarks>
    Custom
}
