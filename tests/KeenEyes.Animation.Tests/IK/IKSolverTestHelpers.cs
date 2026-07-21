using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Shared helpers for building bone entity chains and reading world-space transforms
/// in IK solver tests. Bone Transform3D components are local (parent-relative), so
/// world transforms are composed by walking the entity hierarchy.
/// </summary>
internal static class IKSolverTestHelpers
{
    /// <summary>
    /// Creates a parent-child bone chain from local (parent-relative) positions.
    /// </summary>
    internal static Entity[] CreateChain(World world, params Vector3[] localPositions)
    {
        var bones = new Entity[localPositions.Length];

        for (var i = 0; i < localPositions.Length; i++)
        {
            bones[i] = world.Spawn()
                .With(new Transform3D(localPositions[i], Quaternion.Identity, Vector3.One))
                .Build();

            if (i > 0)
            {
                world.SetParent(bones[i], bones[i - 1]);
            }
        }

        return bones;
    }

    /// <summary>
    /// Computes the world-space transform of an entity by composing local transforms
    /// up the entity hierarchy.
    /// </summary>
    internal static (Vector3 Position, Quaternion Rotation) GetWorldTransform(World world, Entity entity)
    {
        ref readonly var local = ref world.Get<Transform3D>(entity);
        var parent = world.GetParent(entity);

        if (!parent.IsValid || !world.Has<Transform3D>(parent))
        {
            return (local.Position, local.Rotation);
        }

        var (parentPos, parentRot) = GetWorldTransform(world, parent);
        return (
            parentPos + Vector3.Transform(local.Position, parentRot),
            Quaternion.Normalize(parentRot * local.Rotation));
    }

    /// <summary>
    /// Computes the world-space position of an entity.
    /// </summary>
    internal static Vector3 GetWorldPosition(World world, Entity entity)
        => GetWorldTransform(world, entity).Position;

    /// <summary>
    /// Computes the world-space distances between consecutive chain bones.
    /// </summary>
    internal static float[] GetBoneLengths(World world, Entity[] bones)
    {
        var lengths = new float[bones.Length - 1];

        for (var i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Vector3.Distance(
                GetWorldPosition(world, bones[i]),
                GetWorldPosition(world, bones[i + 1]));
        }

        return lengths;
    }
}
