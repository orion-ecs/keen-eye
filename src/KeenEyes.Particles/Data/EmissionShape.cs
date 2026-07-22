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
    Box,

    /// <summary>
    /// Particles emit from within a half-disc.
    /// </summary>
    /// <remarks>
    /// The particle pool is 2D (X/Y only), so a "hemisphere" is interpreted as the 2D
    /// flattening of a hemisphere: a filled half-disc. Emission is restricted to the
    /// 180-degree arc centered on <see cref="EmissionShape.Direction"/>, mirroring the way
    /// <see cref="EmissionShapeType.Sphere"/> flattens a sphere to a filled disc.
    /// </remarks>
    Hemisphere,

    /// <summary>Particles emit along a straight line segment.</summary>
    Edge,

    /// <summary>Particles emit on the perimeter of a circle (a ring, not a filled disc).</summary>
    Circle
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

    /// <summary>
    /// Creates a hemisphere emission shape pointing along the default upward direction.
    /// </summary>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <returns>A hemisphere emission shape.</returns>
    /// <remarks>
    /// In the 2D particle pool this emits from a filled half-disc: positions and initial
    /// directions are sampled across the 180-degree arc centered on
    /// <see cref="Direction"/> (defaulting to <see cref="Vector2.UnitY"/>), consistent with
    /// how <see cref="Sphere"/> flattens a sphere to a filled disc.
    /// </remarks>
    public static EmissionShape Hemisphere(float radius) => new()
    {
        Type = EmissionShapeType.Hemisphere,
        Radius = radius,
        Direction = Vector2.UnitY
    };

    /// <summary>
    /// Creates a hemisphere emission shape oriented along a custom direction.
    /// </summary>
    /// <param name="radius">The radius of the hemisphere.</param>
    /// <param name="direction">The direction the flat side of the hemisphere faces away from (will be normalized).</param>
    /// <returns>A hemisphere emission shape.</returns>
    /// <remarks>
    /// In the 2D particle pool this emits from a filled half-disc: positions and initial
    /// directions are sampled across the 180-degree arc centered on <paramref name="direction"/>.
    /// </remarks>
    public static EmissionShape Hemisphere(float radius, Vector2 direction) => new()
    {
        Type = EmissionShapeType.Hemisphere,
        Radius = radius,
        Direction = Vector2.Normalize(direction)
    };

    /// <summary>
    /// Creates an edge (line segment) emission shape along the X axis.
    /// </summary>
    /// <param name="length">The full length of the segment; particles spawn between <c>-length/2</c> and <c>+length/2</c> on the X axis, relative to the emitter.</param>
    /// <returns>An edge emission shape.</returns>
    /// <remarks>
    /// The segment extent is stored in <see cref="Size"/> as <c>(length, 0)</c>. Particles
    /// spawn uniformly along the segment with a random initial direction.
    /// </remarks>
    public static EmissionShape Edge(float length) => new()
    {
        Type = EmissionShapeType.Edge,
        Size = new Vector2(length, 0f)
    };

    /// <summary>
    /// Creates an edge (line segment) emission shape spanning the given extent vector.
    /// </summary>
    /// <param name="extent">The full segment vector; particles spawn between <c>-extent/2</c> and <c>+extent/2</c>, relative to the emitter.</param>
    /// <returns>An edge emission shape.</returns>
    /// <remarks>
    /// Particles spawn uniformly along the segment with a random initial direction.
    /// </remarks>
    public static EmissionShape Edge(Vector2 extent) => new()
    {
        Type = EmissionShapeType.Edge,
        Size = extent
    };

    /// <summary>
    /// Creates a circle (ring perimeter) emission shape.
    /// </summary>
    /// <param name="radius">The radius of the ring.</param>
    /// <returns>A circle emission shape.</returns>
    /// <remarks>
    /// Particles spawn exactly on the ring's perimeter (distance <paramref name="radius"/>
    /// from the emitter), not within a filled disc, with an outward radial initial direction.
    /// </remarks>
    public static EmissionShape Circle(float radius) => new()
    {
        Type = EmissionShapeType.Circle,
        Radius = radius
    };
}
