using System.Numerics;

namespace KeenEyes.Physics.Components;

/// <summary>
/// Defines the type of collision shape.
/// </summary>
public enum ShapeType
{
    /// <summary>
    /// A sphere shape defined by a radius.
    /// </summary>
    Sphere,

    /// <summary>
    /// A box shape defined by half-extents.
    /// </summary>
    Box,

    /// <summary>
    /// A capsule shape defined by radius and length along an axis.
    /// </summary>
    Capsule,

    /// <summary>
    /// A cylinder shape defined by radius and length.
    /// </summary>
    Cylinder
}

/// <summary>
/// Component that defines the collision shape for a physics-enabled entity.
/// </summary>
/// <remarks>
/// <para>
/// This component must be present alongside <see cref="RigidBody"/> for an entity
/// to participate in the physics simulation. The shape defines how the entity
/// collides with other physics objects.
/// </para>
/// <para>
/// For compound shapes or mesh colliders, consider using multiple entities with
/// parent-child relationships.
/// </para>
/// </remarks>
public struct PhysicsShape : IComponent
{
    /// <summary>
    /// The type of collision shape.
    /// </summary>
    public ShapeType Type;

    /// <summary>
    /// Shape-specific size parameters.
    /// </summary>
    /// <remarks>
    /// <para>Interpretation depends on <see cref="Type"/>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="ShapeType.Sphere"/>: X = radius</description></item>
    /// <item><description><see cref="ShapeType.Box"/>: X/Y/Z = half-extents</description></item>
    /// <item><description><see cref="ShapeType.Capsule"/>: X = radius, Y = half-length</description></item>
    /// <item><description><see cref="ShapeType.Cylinder"/>: X = radius, Y = half-length</description></item>
    /// </list>
    /// </remarks>
    public Vector3 Size;

    /// <summary>
    /// Creates a sphere collision shape.
    /// </summary>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>A sphere PhysicsShape.</returns>
    public static PhysicsShape Sphere(float radius) => new()
    {
        Type = ShapeType.Sphere,
        Size = new Vector3(radius, 0, 0)
    };

    /// <summary>
    /// Creates a box collision shape.
    /// </summary>
    /// <param name="halfExtents">The half-extents (half width, half height, half depth).</param>
    /// <returns>A box PhysicsShape.</returns>
    public static PhysicsShape Box(Vector3 halfExtents) => new()
    {
        Type = ShapeType.Box,
        Size = halfExtents
    };

    /// <summary>
    /// Creates a box collision shape from width, height, and depth.
    /// </summary>
    /// <param name="width">The full width of the box.</param>
    /// <param name="height">The full height of the box.</param>
    /// <param name="depth">The full depth of the box.</param>
    /// <returns>A box PhysicsShape.</returns>
    public static PhysicsShape Box(float width, float height, float depth) => new()
    {
        Type = ShapeType.Box,
        Size = new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f)
    };

    /// <summary>
    /// Creates a capsule collision shape oriented along the Y axis.
    /// </summary>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="length">The total length of the capsule (including end caps).</param>
    /// <returns>A capsule PhysicsShape.</returns>
    public static PhysicsShape Capsule(float radius, float length) => new()
    {
        Type = ShapeType.Capsule,
        Size = new Vector3(radius, (length - 2 * radius) * 0.5f, 0)
    };

    /// <summary>
    /// Creates a cylinder collision shape oriented along the Y axis.
    /// </summary>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="length">The total length of the cylinder.</param>
    /// <returns>A cylinder PhysicsShape.</returns>
    public static PhysicsShape Cylinder(float radius, float length) => new()
    {
        Type = ShapeType.Cylinder,
        Size = new Vector3(radius, length * 0.5f, 0)
    };
}
