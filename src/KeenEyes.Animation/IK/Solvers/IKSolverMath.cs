using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Shared math helpers for IK solvers working on bone entity hierarchies.
/// </summary>
/// <remarks>
/// <para>
/// Bone <see cref="Transform3D"/> components store LOCAL (parent-relative) transforms:
/// <c>SkinnedMeshBoneSystem</c> composes each bone's local matrix with its parent's world
/// matrix by walking <see cref="IWorld.GetParent"/>, and <c>SkeletonPoseSystem</c> writes
/// parent-relative animation track samples directly into bone transforms. Solvers therefore
/// compose world-space positions by walking the entity hierarchy and write results back as
/// local rotations relative to each bone's parent.
/// </para>
/// <para>
/// All helpers assume chain bones form a parent-child hierarchy from root to tip (each bone's
/// entity parent is the previous chain bone; the root bone's parent may be any entity).
/// </para>
/// </remarks>
internal static class IKSolverMath
{
    /// <summary>
    /// Computes the world-space transform of an entity by composing local transforms
    /// up the entity hierarchy.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to compute the world transform for.</param>
    /// <returns>The world-space position, rotation, and scale.</returns>
    internal static (Vector3 Position, Quaternion Rotation, Vector3 Scale) GetWorldTransform(
        IWorld world, Entity entity)
    {
        ref readonly var local = ref world.Get<Transform3D>(entity);
        var parent = world.GetParent(entity);

        if (!parent.IsValid || !world.Has<Transform3D>(parent))
        {
            return (local.Position, local.Rotation, local.Scale);
        }

        var (parentPos, parentRot, parentScale) = GetWorldTransform(world, parent);

        return (
            parentPos + Vector3.Transform(parentScale * local.Position, parentRot),
            Quaternion.Normalize(parentRot * local.Rotation),
            parentScale * local.Scale);
    }

    /// <summary>
    /// Fills <paramref name="positions"/> with the world-space positions of the chain bones.
    /// </summary>
    /// <param name="world">The world containing the bones.</param>
    /// <param name="bones">Bone entities ordered root to tip.</param>
    /// <param name="positions">Destination span; must be at least as long as <paramref name="bones"/>.</param>
    internal static void GetWorldPositions(IWorld world, Entity[] bones, Span<Vector3> positions)
    {
        for (var i = 0; i < bones.Length; i++)
        {
            positions[i] = GetWorldTransform(world, bones[i]).Position;
        }
    }

    /// <summary>
    /// Computes the shortest-arc rotation mapping one unit direction onto another.
    /// </summary>
    /// <param name="from">The normalized source direction.</param>
    /// <param name="to">The normalized destination direction.</param>
    /// <returns>A quaternion rotating <paramref name="from"/> onto <paramref name="to"/>.</returns>
    internal static Quaternion RotationBetween(Vector3 from, Vector3 to)
    {
        var dot = Vector3.Dot(from, to);

        if (dot > 1f - FloatExtensions.DefaultEpsilon)
        {
            return Quaternion.Identity;
        }

        if (dot < -1f + FloatExtensions.DefaultEpsilon)
        {
            // Opposite directions: rotate 180 degrees about any perpendicular axis.
            return Quaternion.CreateFromAxisAngle(AnyPerpendicular(from), MathF.PI);
        }

        var axis = Vector3.Normalize(Vector3.Cross(from, to));
        var angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return Quaternion.CreateFromAxisAngle(axis, angle);
    }

    /// <summary>
    /// Returns an arbitrary unit vector perpendicular to the given direction.
    /// </summary>
    /// <param name="direction">The reference direction.</param>
    /// <returns>A normalized vector perpendicular to <paramref name="direction"/>.</returns>
    internal static Vector3 AnyPerpendicular(Vector3 direction)
    {
        var axis = Vector3.Cross(direction, Vector3.UnitX);
        if (axis.LengthSquared().IsApproximatelyZero())
        {
            axis = Vector3.Cross(direction, Vector3.UnitY);
        }

        return Vector3.Normalize(axis);
    }

    /// <summary>
    /// Writes solved world-space joint positions back to the chain as local bone rotations.
    /// </summary>
    /// <remarks>
    /// Only rotations are modified: each bone is rotated in world space so that its child lands
    /// on the solved position, then converted back to a parent-relative local rotation. Because
    /// solvers preserve joint distances, bone translation offsets (and thus bone lengths) are
    /// left untouched.
    /// </remarks>
    /// <param name="world">The world containing the bones.</param>
    /// <param name="bones">Bone entities ordered root to tip, forming a parent-child hierarchy.</param>
    /// <param name="solved">Solved world-space positions for each bone, root to tip.</param>
    /// <param name="tipWorldRotation">Optional world-space rotation to apply to the tip bone (end effector).</param>
    internal static void ApplyWorldPositions(
        IWorld world, Entity[] bones, ReadOnlySpan<Vector3> solved, Quaternion? tipWorldRotation)
    {
        // World transform of the chain root's parent (identity if none).
        var parentPos = Vector3.Zero;
        var parentRot = Quaternion.Identity;
        var parentScale = Vector3.One;

        var rootParent = world.GetParent(bones[0]);
        if (rootParent.IsValid && world.Has<Transform3D>(rootParent))
        {
            (parentPos, parentRot, parentScale) = GetWorldTransform(world, rootParent);
        }

        for (var i = 0; i < bones.Length - 1; i++)
        {
            ref var local = ref world.Get<Transform3D>(bones[i]);
            var worldRot = Quaternion.Normalize(parentRot * local.Rotation);
            var worldPos = parentPos + Vector3.Transform(parentScale * local.Position, parentRot);
            var worldScale = parentScale * local.Scale;

            ref readonly var childLocal = ref world.Get<Transform3D>(bones[i + 1]);
            var childWorldPos = worldPos + Vector3.Transform(worldScale * childLocal.Position, worldRot);

            var currentDir = childWorldPos - worldPos;
            var desiredDir = solved[i + 1] - solved[i];

            if (!currentDir.LengthSquared().IsApproximatelyZero() &&
                !desiredDir.LengthSquared().IsApproximatelyZero())
            {
                var delta = RotationBetween(
                    Vector3.Normalize(currentDir),
                    Vector3.Normalize(desiredDir));
                worldRot = Quaternion.Normalize(delta * worldRot);
                local.Rotation = Quaternion.Normalize(Quaternion.Inverse(parentRot) * worldRot);
            }

            parentPos = worldPos;
            parentRot = worldRot;
            parentScale = worldScale;
        }

        if (tipWorldRotation is { } targetRotation)
        {
            ref var tipLocal = ref world.Get<Transform3D>(bones[^1]);
            tipLocal.Rotation = Quaternion.Normalize(Quaternion.Inverse(parentRot) * targetRotation);
        }
    }
}
