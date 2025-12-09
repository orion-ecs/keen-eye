using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// 3D transformation component containing position, rotation, and scale.
/// </summary>
/// <remarks>
/// <para>
/// This component uses <see cref="System.Numerics"/> types for SIMD acceleration.
/// The transformation matrix is computed on-demand to avoid unnecessary calculations.
/// </para>
/// <para>
/// For optimal cache performance, consider using separate Position, Rotation, and Scale
/// components if only subsets are frequently accessed.
/// </para>
/// </remarks>
public struct Transform3D : IComponent
{
    /// <summary>
    /// The world position of the entity.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The rotation of the entity as a quaternion.
    /// </summary>
    public Quaternion Rotation;

    /// <summary>
    /// The scale of the entity.
    /// </summary>
    public Vector3 Scale;

    /// <summary>
    /// Creates a new transform with the specified values.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation quaternion.</param>
    /// <param name="scale">The scale.</param>
    public Transform3D(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    /// <summary>
    /// Creates a new transform at the origin with identity rotation and unit scale.
    /// </summary>
    public static Transform3D Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    /// <summary>
    /// Computes the 4x4 transformation matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <returns>The world transformation matrix.</returns>
    public readonly Matrix4x4 ToMatrix()
    {
        return Matrix4x4.CreateScale(Scale) *
               Matrix4x4.CreateFromQuaternion(Rotation) *
               Matrix4x4.CreateTranslation(Position);
    }

    /// <summary>
    /// Gets the forward direction vector (negative Z-axis after rotation).
    /// </summary>
    public readonly Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

    /// <summary>
    /// Gets the right direction vector (positive X-axis after rotation).
    /// </summary>
    public readonly Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);

    /// <summary>
    /// Gets the up direction vector (positive Y-axis after rotation).
    /// </summary>
    public readonly Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
}
