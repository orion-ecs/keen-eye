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
public struct Transform2D : IComponent
{
    /// <summary>
    /// The world position of the entity in 2D space.
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The rotation of the entity in radians. Positive values rotate counter-clockwise.
    /// </summary>
    public float Rotation;

    /// <summary>
    /// The scale of the entity.
    /// </summary>
    public Vector2 Scale;

    /// <summary>
    /// Creates a new 2D transform with the specified values.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale.</param>
    public Transform2D(Vector2 position, float rotation, Vector2 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    /// <summary>
    /// Creates a new transform at the origin with zero rotation and unit scale.
    /// </summary>
    public static Transform2D Identity => new(Vector2.Zero, 0f, Vector2.One);

    /// <summary>
    /// Computes the 3x2 transformation matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <returns>The world transformation matrix.</returns>
    public readonly Matrix3x2 ToMatrix()
    {
        return Matrix3x2.CreateScale(Scale) *
               Matrix3x2.CreateRotation(Rotation) *
               Matrix3x2.CreateTranslation(Position);
    }

    /// <summary>
    /// Gets the forward direction vector (unit vector in the direction of rotation).
    /// </summary>
    public readonly Vector2 Forward => new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));

    /// <summary>
    /// Gets the right direction vector (perpendicular to forward, 90 degrees clockwise).
    /// </summary>
    public readonly Vector2 Right => new((float)Math.Sin(Rotation), -(float)Math.Cos(Rotation));
}
