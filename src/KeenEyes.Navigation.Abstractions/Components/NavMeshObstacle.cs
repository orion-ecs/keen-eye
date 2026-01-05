using System.Numerics;

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component for dynamic obstacles that affect navigation mesh carving.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that should dynamically block
/// navigation paths. The navigation system will carve holes in the
/// navmesh around these obstacles.
/// </para>
/// <para>
/// Use <see cref="Carving"/> to enable/disable runtime navmesh modification.
/// Carving is more expensive but provides more accurate pathfinding.
/// Non-carving obstacles are used for local avoidance only.
/// </para>
/// </remarks>
public struct NavMeshObstacle : IComponent
{
    /// <summary>
    /// The shape of the obstacle.
    /// </summary>
    public ObstacleShape Shape;

    /// <summary>
    /// The center offset from the entity's position.
    /// </summary>
    public Vector3 Center;

    /// <summary>
    /// The size of a box-shaped obstacle (width, height, depth).
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="Shape"/> is <see cref="ObstacleShape.Box"/>.
    /// </remarks>
    public Vector3 Size;

    /// <summary>
    /// The radius of a cylindrical obstacle.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="Shape"/> is <see cref="ObstacleShape.Cylinder"/>.
    /// </remarks>
    public float Radius;

    /// <summary>
    /// The height of a cylindrical obstacle.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="Shape"/> is <see cref="ObstacleShape.Cylinder"/>.
    /// </remarks>
    public float Height;

    /// <summary>
    /// Whether this obstacle should carve holes in the navigation mesh.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the navmesh is dynamically modified around this obstacle.
    /// Paths will avoid the carved area entirely.
    /// </para>
    /// <para>
    /// When false, the obstacle is only used for local avoidance.
    /// Agents will still attempt to path through the area but avoid
    /// collisions at runtime.
    /// </para>
    /// </remarks>
    public bool Carving;

    /// <summary>
    /// The time in seconds to wait before carving after the obstacle moves.
    /// </summary>
    /// <remarks>
    /// Higher values reduce carving updates for moving obstacles but
    /// may cause temporary path inaccuracies.
    /// </remarks>
    public float CarvingMoveThreshold;

    /// <summary>
    /// The distance the obstacle must move before the carve is updated.
    /// </summary>
    public float CarvingTimeToStationary;

    /// <summary>
    /// Creates a box-shaped obstacle.
    /// </summary>
    /// <param name="size">The size (width, height, depth) of the box.</param>
    /// <param name="carving">Whether the obstacle carves the navmesh.</param>
    /// <returns>A new NavMeshObstacle component.</returns>
    public static NavMeshObstacle Box(Vector3 size, bool carving = true)
        => new()
        {
            Shape = ObstacleShape.Box,
            Size = size,
            Carving = carving,
            CarvingMoveThreshold = 0.1f,
            CarvingTimeToStationary = 0.5f
        };

    /// <summary>
    /// Creates a cylindrical obstacle.
    /// </summary>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="carving">Whether the obstacle carves the navmesh.</param>
    /// <returns>A new NavMeshObstacle component.</returns>
    public static NavMeshObstacle Cylinder(float radius, float height, bool carving = true)
        => new()
        {
            Shape = ObstacleShape.Cylinder,
            Radius = radius,
            Height = height,
            Carving = carving,
            CarvingMoveThreshold = 0.1f,
            CarvingTimeToStationary = 0.5f
        };
}
