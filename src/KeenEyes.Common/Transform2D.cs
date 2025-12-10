using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// 2D transformation component containing position, rotation, and scale.
/// </summary>
/// <remarks>
/// <para>
/// This component uses <see cref="System.Numerics"/> types for SIMD acceleration.
/// The transformation matrix is computed on-demand to avoid unnecessary calculations.
/// </para>
/// <para>
/// Rotation is stored in radians. Positive values rotate counter-clockwise.
/// </para>
/// </remarks>
/// <param name="position">The world position.</param>
/// <param name="rotation">The rotation in radians.</param>
/// <param name="scale">The scale.</param>
public struct Transform2D(Vector2 position, float rotation, Vector2 scale) : IComponent
{
    /// <summary>
    /// The world position of the entity in 2D space.
    /// </summary>
    public Vector2 Position = position;

    /// <summary>
    /// The rotation of the entity in radians. Positive values rotate counter-clockwise.
    /// </summary>
    public float Rotation = rotation;

    /// <summary>
    /// The scale of the entity.
    /// </summary>
    public Vector2 Scale = scale;

    /// <summary>
    /// Creates a new transform at the origin with zero rotation and unit scale.
    /// </summary>
    public static Transform2D Identity => new(Vector2.Zero, 0f, Vector2.One);
}
