using System.Numerics;

namespace KeenEyes.Particles.Data;

/// <summary>
/// The type of shape from which particles are emitted.
/// </summary>
public enum EmissionShapeType
{
    /// <summary>Particles emit from a single point.</summary>
    Point,

    /// <summary>Particles emit from within a sphere.</summary>
    Sphere,

    /// <summary>Particles emit in a cone pattern.</summary>
    Cone,

    /// <summary>Particles emit from within a rectangular box.</summary>
    Box
}

/// <summary>
/// Defines the shape from which particles are emitted.
/// </summary>
public readonly record struct EmissionShape
{
    /// <summary>
    /// Gets the type of emission shape.
    /// </summary>
    public EmissionShapeType Type { get; init; }

    /// <summary>
    /// Gets the radius for sphere and cone shapes.
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// Gets the angle in radians for cone shapes (total spread angle).
    /// </summary>
    public float Angle { get; init; }

    /// <summary>
    /// Gets the size for box shapes (width, height).
    /// </summary>
    public Vector2 Size { get; init; }

    /// <summary>
    /// Gets the direction for directional emission (normalized).
    /// </summary>
    public Vector2 Direction { get; init; }

    /// <summary>
    /// Creates a point emission shape where all particles emit from the origin.
    /// </summary>
    public static EmissionShape Point => new() { Type = EmissionShapeType.Point };

    /// <summary>
    /// Creates a spherical emission shape.
    /// </summary>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>A sphere emission shape.</returns>
    public static EmissionShape Sphere(float radius) => new()
    {
        Type = EmissionShapeType.Sphere,
        Radius = radius
    };

    /// <summary>
    /// Creates a cone emission shape.
    /// </summary>
    /// <param name="radius">The base radius of the cone.</param>
    /// <param name="angle">The total spread angle in radians.</param>
    /// <returns>A cone emission shape.</returns>
    public static EmissionShape Cone(float radius, float angle) => new()
    {
        Type = EmissionShapeType.Cone,
        Radius = radius,
        Angle = angle,
        Direction = Vector2.UnitY // Default upward
    };

    /// <summary>
    /// Creates a cone emission shape with a custom direction.
    /// </summary>
    /// <param name="radius">The base radius of the cone.</param>
    /// <param name="angle">The total spread angle in radians.</param>
    /// <param name="direction">The center direction of the cone (will be normalized).</param>
    /// <returns>A cone emission shape.</returns>
    public static EmissionShape Cone(float radius, float angle, Vector2 direction) => new()
    {
        Type = EmissionShapeType.Cone,
        Radius = radius,
        Angle = angle,
        Direction = Vector2.Normalize(direction)
    };

    /// <summary>
    /// Creates a box emission shape.
    /// </summary>
    /// <param name="width">The width of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <returns>A box emission shape.</returns>
    public static EmissionShape Box(float width, float height) => new()
    {
        Type = EmissionShapeType.Box,
        Size = new Vector2(width, height)
    };
}
