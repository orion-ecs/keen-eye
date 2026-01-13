using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Common;

namespace KeenEyes.Animation;

/// <summary>
/// Data for a single bone used when instantiating a skeleton hierarchy.
/// </summary>
/// <param name="Name">The bone name (used to match animation channels).</param>
/// <param name="ParentIndex">Index of the parent bone, or -1 for root bones.</param>
/// <param name="LocalBindPose">The local transform of this bone in bind pose.</param>
public readonly record struct BoneSetupData(
    string Name,
    int ParentIndex,
    Matrix4x4 LocalBindPose);

/// <summary>
/// Factory methods for instantiating skeleton bone entities.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SkeletonFactory"/> provides helper methods to create the entity hierarchy
/// needed for skeletal animation. It creates bone entities with:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Transform3D"/> - Initialized to bind pose</description></item>
///   <item><description><see cref="BoneReference"/> - Links bone to skeleton root</description></item>
///   <item><description>Parent-child relationships matching skeleton hierarchy</description></item>
/// </list>
/// <para>
/// Use this factory to quickly set up a skeleton hierarchy from a loaded model,
/// rather than manually creating each bone entity.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load a model with skeleton
/// var model = assetManager.Load&lt;ModelAsset&gt;("character.glb");
///
/// // Create the character entity
/// var character = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new AnimationPlayer { ClipId = walkClipId })
///     .Build();
///
/// // Convert SkeletonAsset bone data to BoneSetupData
/// var bones = model.Skeleton.Bones.Select(b =&gt;
///     new BoneSetupData(b.Name, b.ParentIndex, b.LocalBindPose)).ToArray();
///
/// // Instantiate the skeleton hierarchy
/// var boneEntities = SkeletonFactory.InstantiateSkeleton(world, bones, character);
///
/// // Create the skinned mesh component
/// world.Spawn()
///     .With(Transform3D.Identity)
///     .With(SkinnedMesh.Create(meshId, boneEntities, model.Skeleton.InverseBindMatrices))
///     .Build();
/// </code>
/// </example>
public static class SkeletonFactory
{
    /// <summary>
    /// Creates bone entities for a skeleton and attaches them to a root entity.
    /// </summary>
    /// <param name="world">The world to create entities in.</param>
    /// <param name="bones">The bone setup data defining the hierarchy.</param>
    /// <param name="rootEntity">The root entity that owns the skeleton (typically has AnimationPlayer).</param>
    /// <returns>Array of bone entity IDs in skeleton order (indices match bone array).</returns>
    /// <exception cref="ArgumentNullException">Thrown when bones is null.</exception>
    /// <remarks>
    /// <para>
    /// The returned array contains entity IDs indexed by bone index. Use this array
    /// when creating a <see cref="SkinnedMesh"/> component's BoneEntityIds.
    /// </para>
    /// <para>
    /// Bone entities are created as children of the root entity, with the bone hierarchy
    /// reproduced through parent-child relationships.
    /// </para>
    /// </remarks>
    public static int[] InstantiateSkeleton(IWorld world, BoneSetupData[] bones, Entity rootEntity)
    {
        ArgumentNullException.ThrowIfNull(bones);

        var boneEntities = new Entity[bones.Length];
        var boneEntityIds = new int[bones.Length];

        // Create all bone entities first (we need them all before setting up hierarchy)
        for (var i = 0; i < bones.Length; i++)
        {
            var boneData = bones[i];

            // Decompose the bind pose matrix to Transform3D components
            DecomposeMatrix(boneData.LocalBindPose, out var position, out var rotation, out var scale);

            var boneEntity = world.Spawn()
                .With(new Transform3D
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = scale
                })
                .With(new BoneReference
                {
                    BoneName = boneData.Name,
                    SkeletonRootId = rootEntity.Id
                })
                .Build();

            boneEntities[i] = boneEntity;
            boneEntityIds[i] = boneEntity.Id;
        }

        // Set up parent-child relationships
        for (var i = 0; i < bones.Length; i++)
        {
            var boneData = bones[i];

            if (boneData.ParentIndex >= 0 && boneData.ParentIndex < bones.Length)
            {
                // Child of another bone
                world.SetParent(boneEntities[i], boneEntities[boneData.ParentIndex]);
            }
            else
            {
                // Root bone - child of the skeleton root entity
                world.SetParent(boneEntities[i], rootEntity);
            }
        }

        return boneEntityIds;
    }

    /// <summary>
    /// Creates bone entities for a skeleton without attaching to a root entity.
    /// </summary>
    /// <param name="world">The world to create entities in.</param>
    /// <param name="bones">The bone setup data defining the hierarchy.</param>
    /// <param name="skeletonRootId">The entity ID to reference as skeleton root (for BoneReference).</param>
    /// <returns>Array of bone entity IDs in skeleton order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bones is null.</exception>
    /// <remarks>
    /// Use this overload when you want to manage the root entity hierarchy separately,
    /// or when the root entity doesn't exist yet.
    /// </remarks>
    public static int[] InstantiateSkeletonBones(IWorld world, BoneSetupData[] bones, int skeletonRootId)
    {
        ArgumentNullException.ThrowIfNull(bones);

        var boneEntities = new Entity[bones.Length];
        var boneEntityIds = new int[bones.Length];

        // Create all bone entities
        for (var i = 0; i < bones.Length; i++)
        {
            var boneData = bones[i];
            DecomposeMatrix(boneData.LocalBindPose, out var position, out var rotation, out var scale);

            var boneEntity = world.Spawn()
                .With(new Transform3D
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = scale
                })
                .With(new BoneReference
                {
                    BoneName = boneData.Name,
                    SkeletonRootId = skeletonRootId
                })
                .Build();

            boneEntities[i] = boneEntity;
            boneEntityIds[i] = boneEntity.Id;
        }

        // Set up hierarchy between bones only (no root entity connection)
        for (var i = 0; i < bones.Length; i++)
        {
            var parentIndex = bones[i].ParentIndex;
            if (parentIndex >= 0 && parentIndex < bones.Length)
            {
                world.SetParent(boneEntities[i], boneEntities[parentIndex]);
            }
        }

        return boneEntityIds;
    }

    /// <summary>
    /// Despawns all bone entities for a skeleton.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="boneEntityIds">The bone entity IDs to despawn.</param>
    /// <remarks>
    /// This is a convenience method for cleaning up skeleton entities when
    /// a character is removed from the scene.
    /// </remarks>
    public static void DespawnSkeleton(IWorld world, int[]? boneEntityIds)
    {
        if (boneEntityIds is null)
        {
            return;
        }

        foreach (var entityId in boneEntityIds)
        {
            var entity = new Entity(entityId, 0);
            if (world.IsAlive(entity))
            {
                world.Despawn(entity);
            }
        }
    }

    /// <summary>
    /// Finds a bone entity by name within a skeleton's bone entities.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="boneEntityIds">The bone entity IDs to search.</param>
    /// <param name="boneName">The bone name to find.</param>
    /// <returns>The bone entity, or an invalid entity if not found.</returns>
    public static Entity FindBoneByName(IWorld world, int[]? boneEntityIds, string boneName)
    {
        if (boneEntityIds is null || string.IsNullOrEmpty(boneName))
        {
            return default;
        }

        foreach (var entityId in boneEntityIds)
        {
            var entity = new Entity(entityId, 0);
            if (!world.IsAlive(entity) || !world.Has<BoneReference>(entity))
            {
                continue;
            }

            ref readonly var boneRef = ref world.Get<BoneReference>(entity);
            if (string.Equals(boneRef.BoneName, boneName, StringComparison.Ordinal))
            {
                return entity;
            }
        }

        return default;
    }

    /// <summary>
    /// Decomposes a transformation matrix into position, rotation, and scale.
    /// </summary>
    private static void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        if (Matrix4x4.Decompose(matrix, out scale, out rotation, out position))
        {
            return;
        }

        // Fallback if decomposition fails
        position = new Vector3(matrix.M41, matrix.M42, matrix.M43);
        rotation = Quaternion.Identity;
        scale = Vector3.One;
    }
}
