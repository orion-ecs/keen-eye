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
}
