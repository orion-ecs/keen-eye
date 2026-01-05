namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Shape of a dynamic navigation obstacle for navmesh carving.
/// </summary>
public enum ObstacleShape
{
    /// <summary>
    /// A box-shaped obstacle defined by width, height, and depth.
    /// </summary>
    /// <remarks>
    /// Use for rectangular objects like crates, buildings, or walls.
    /// </remarks>
    Box,

    /// <summary>
    /// A cylindrical obstacle defined by radius and height.
    /// </summary>
    /// <remarks>
    /// Use for round objects like barrels, pillars, or characters.
    /// </remarks>
    Cylinder
}
