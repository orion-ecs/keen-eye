using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.Rendering;
using KeenEyes.Common;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that computes bone matrices for skinned mesh rendering.
/// </summary>
/// <remarks>
/// <para>
/// This system queries entities with <see cref="SkinnedMesh"/> components and computes
/// the final bone matrices needed for GPU skinning. For each skinned mesh:
/// </para>
/// <list type="number">
///   <item><description>Reads world transforms from bone entities</description></item>
///   <item><description>Gets inverse bind matrices from the SkinnedMesh component</description></item>
///   <item><description>Computes: boneMatrix = boneWorldTransform × inverseBindMatrix</description></item>
///   <item><description>Stores results in a <see cref="BoneMatrixBuffer"/> for GPU upload</description></item>
/// </list>
/// <para>
/// The computed bone matrices are stored in a dictionary keyed by entity ID for the
/// rendering system to access. Use <see cref="GetBoneMatrixBuffer"/> to retrieve the
/// buffer for a specific skinned mesh entity.
/// </para>
/// </remarks>
public sealed class SkinnedMeshBoneSystem : SystemBase
{
    private readonly Dictionary<int, BoneMatrixBuffer> boneBuffers = [];
    private ulong currentGeneration;

    /// <summary>
    /// Gets the bone matrix buffer for a skinned mesh entity.
    /// </summary>
    /// <param name="entityId">The skinned mesh entity ID.</param>
    /// <returns>The bone matrix buffer, or null if not found.</returns>
    public BoneMatrixBuffer? GetBoneMatrixBuffer(int entityId)
    {
        return boneBuffers.TryGetValue(entityId, out var buffer) ? buffer : null;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Increment generation for dirty tracking
        currentGeneration++;

        // Track which entities we've seen this frame
        var activeEntities = new HashSet<int>();

        // Process all skinned mesh entities
        foreach (var entity in World.Query<SkinnedMesh, Transform3D>())
        {
            activeEntities.Add(entity.Id);

            ref readonly var skinnedMesh = ref World.Get<SkinnedMesh>(entity);

            // Get or create bone matrix buffer for this entity
            if (!boneBuffers.TryGetValue(entity.Id, out var boneBuffer))
            {
                boneBuffer = new BoneMatrixBuffer();
                boneBuffers[entity.Id] = boneBuffer;
            }

            // Compute bone matrices using the inverse bind matrices on the component
            ComputeBoneMatrices(skinnedMesh, boneBuffer);
        }

        // Clean up buffers for despawned entities
        var entitiesToRemove = new List<int>();
        foreach (var entityId in boneBuffers.Keys)
        {
            if (!activeEntities.Contains(entityId))
            {
                entitiesToRemove.Add(entityId);
            }
        }

        foreach (var entityId in entitiesToRemove)
        {
            if (boneBuffers.TryGetValue(entityId, out var buffer))
            {
                buffer.Dispose();
                boneBuffers.Remove(entityId);
            }
        }
    }

    private void ComputeBoneMatrices(in SkinnedMesh skinnedMesh, BoneMatrixBuffer buffer)
    {
        if (skinnedMesh.BoneEntityIds is null || skinnedMesh.BoneEntityIds.Length == 0 ||
            skinnedMesh.InverseBindMatrices is null || skinnedMesh.InverseBindMatrices.Length == 0)
        {
            return;
        }

        var boneCount = Math.Min(skinnedMesh.BoneEntityIds.Length, skinnedMesh.InverseBindMatrices.Length);
        boneCount = Math.Min(boneCount, buffer.MaxBones);

        for (var i = 0; i < boneCount; i++)
        {
            var boneEntityId = skinnedMesh.BoneEntityIds[i];

            // Get world transform of the bone entity
            var boneWorldMatrix = GetBoneWorldMatrix(boneEntityId);

            // Compute final bone matrix: worldTransform * inverseBindMatrix
            var inverseBindMatrix = skinnedMesh.InverseBindMatrices[i];
            var finalMatrix = boneWorldMatrix * inverseBindMatrix;

            // Store in buffer with current generation for dirty tracking
            buffer.SetBoneMatrix(i, finalMatrix, currentGeneration);
        }
    }

    private Matrix4x4 GetBoneWorldMatrix(int boneEntityId)
    {
        // Try to get the entity
        var entity = new Entity(boneEntityId, 0); // Version 0 for lookup

        // Check if entity is alive
        if (!World.IsAlive(entity))
        {
            return Matrix4x4.Identity;
        }

        // Check if entity has Transform3D
        if (!World.Has<Transform3D>(entity))
        {
            return Matrix4x4.Identity;
        }

        ref readonly var transform = ref World.Get<Transform3D>(entity);

        // Build the local matrix from transform components
        var localMatrix = Matrix4x4.CreateScale(transform.Scale) *
                         Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                         Matrix4x4.CreateTranslation(transform.Position);

        // If entity has a parent, multiply by parent's world matrix
        var parentEntity = World.GetParent(entity);
        if (parentEntity.IsValid)
        {
            var parentWorld = GetBoneWorldMatrix(parentEntity.Id);
            return localMatrix * parentWorld;
        }

        return localMatrix;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var buffer in boneBuffers.Values)
            {
                buffer.Dispose();
            }

            boneBuffers.Clear();
        }

        base.Dispose(disposing);
    }
}
