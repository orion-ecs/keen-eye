using System.Numerics;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Component that identifies a mesh entity as a skinned (skeletal animated) mesh.
/// </summary>
/// <remarks>
/// <para>
/// Entities with SkinnedMesh have their vertices transformed by bone matrices during rendering.
/// The component references a skeleton and tracks which bone entities correspond to each joint.
/// </para>
/// <para>
/// The skinning system reads bone transforms from the bone entity hierarchy and computes
/// the final bone matrices (boneWorldTransform * inverseBindMatrix) for GPU skinning.
/// </para>
/// <para>
/// Prerequisites for a skinned mesh entity:
/// <list type="bullet">
///   <item><description>MeshAsset with joint indices and weights in vertex data</description></item>
///   <item><description>InverseBindMatrices populated with skeleton data</description></item>
///   <item><description>Bone entities with BoneReference components</description></item>
///   <item><description>Transform3D component for world position</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a skinned mesh entity with skeleton data
/// var character = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(SkinnedMesh.Create(meshHandle.Id, boneEntities, skeleton.InverseBindMatrices))
///     .With(new AnimationPlayer { Clip = walkClip })
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct SkinnedMesh
{
    /// <summary>
    /// The asset handle ID for the mesh being rendered.
    /// </summary>
    /// <remarks>
    /// The mesh must have joint indices and weights in its vertex data.
    /// These are populated when loading skinned meshes from glTF files.
    /// </remarks>
    public int MeshAssetId;

    /// <summary>
    /// Entity IDs of the bone entities in skeleton order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The array indices correspond to bone indices in the skeleton.
    /// Each bone entity should have a BoneReference and Transform3D component.
    /// </para>
    /// <para>
    /// The skinning system reads world transforms from these entities
    /// and multiplies by the inverse bind matrices to compute
    /// the final bone matrices for GPU upload.
    /// </para>
    /// </remarks>
    public int[]? BoneEntityIds;

    /// <summary>
    /// The inverse bind matrices for GPU skinning.
    /// </summary>
    /// <remarks>
    /// Each inverse bind matrix transforms vertices from model space to the
    /// corresponding bone's local space. During rendering, the final skinning
    /// matrix is computed as: boneWorldTransform * inverseBindMatrix.
    /// </remarks>
    public Matrix4x4[]? InverseBindMatrices;

    /// <summary>
    /// Generation counter for dirty tracking.
    /// </summary>
    /// <remarks>
    /// Incremented when bone assignments change. Used by the render system
    /// to detect when bone matrix buffer needs full recomputation.
    /// </remarks>
    public ulong Generation;

    /// <summary>
    /// Creates a skinned mesh component with the specified mesh asset and bone data.
    /// </summary>
    /// <param name="meshAssetId">The mesh asset handle ID.</param>
    /// <param name="boneEntityIds">The bone entity IDs in skeleton order.</param>
    /// <param name="inverseBindMatrices">The inverse bind matrices from the skeleton.</param>
    /// <returns>A configured skinned mesh component.</returns>
    public static SkinnedMesh Create(int meshAssetId, int[] boneEntityIds, Matrix4x4[] inverseBindMatrices) => new()
    {
        MeshAssetId = meshAssetId,
        BoneEntityIds = boneEntityIds,
        InverseBindMatrices = inverseBindMatrices,
        Generation = 1
    };
}
