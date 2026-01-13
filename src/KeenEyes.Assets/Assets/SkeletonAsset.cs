using System.Numerics;

namespace KeenEyes.Assets;

/// <summary>
/// Data for a single bone in a skeleton hierarchy.
/// </summary>
/// <param name="Name">The bone name (used to match animation channels).</param>
/// <param name="ParentIndex">Index of the parent bone, or -1 for root bones.</param>
/// <param name="LocalBindPose">The local transform of this bone in bind pose.</param>
public readonly record struct BoneData(
    string Name,
    int ParentIndex,
    Matrix4x4 LocalBindPose);

/// <summary>
/// A skeleton asset containing bone hierarchy and inverse bind matrices for skinning.
/// </summary>
/// <remarks>
/// <para>
/// The skeleton defines the bone hierarchy used for skeletal animation. It is loaded
/// from glTF skin data and contains:
/// </para>
/// <list type="bullet">
/// <item><description>Bone hierarchy (parent-child relationships)</description></item>
/// <item><description>Inverse bind matrices for GPU skinning</description></item>
/// <item><description>Bind pose transforms for each bone</description></item>
/// </list>
/// <para>
/// Bones are stored in hierarchy order (parents before children) to enable
/// efficient transform propagation. The root bone has a parent index of -1.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load a model with skeleton
/// var model = assetManager.Load&lt;ModelAsset&gt;("character.glb");
/// var skeleton = model.Skeleton;
///
/// // Instantiate skeleton entities
/// var rootEntity = SkeletonFactory.InstantiateSkeleton(world, skeleton, characterEntity);
/// </code>
/// </example>
public sealed class SkeletonAsset : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the name of this skeleton.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets all bones in the skeleton, ordered by hierarchy (parents before children).
    /// </summary>
    public BoneData[] Bones { get; }

    /// <summary>
    /// Gets the inverse bind matrices for GPU skinning.
    /// </summary>
    /// <remarks>
    /// Each inverse bind matrix transforms vertices from model space to the
    /// corresponding bone's local space. During rendering, the final skinning
    /// matrix is computed as: boneWorldTransform * inverseBindMatrix.
    /// </remarks>
    public Matrix4x4[] InverseBindMatrices { get; }

    /// <summary>
    /// Gets the index of the root bone in the <see cref="Bones"/> array.
    /// </summary>
    public int RootBoneIndex { get; }

    /// <summary>
    /// Gets the number of bones in this skeleton.
    /// </summary>
    public int BoneCount => Bones.Length;

    /// <summary>
    /// Creates a new skeleton asset.
    /// </summary>
    /// <param name="name">The skeleton name.</param>
    /// <param name="bones">The bone data array.</param>
    /// <param name="inverseBindMatrices">The inverse bind matrices.</param>
    /// <param name="rootBoneIndex">The index of the root bone.</param>
    /// <exception cref="ArgumentNullException">Thrown when bones or matrices are null.</exception>
    /// <exception cref="ArgumentException">Thrown when arrays have mismatched lengths.</exception>
    public SkeletonAsset(
        string name,
        BoneData[] bones,
        Matrix4x4[] inverseBindMatrices,
        int rootBoneIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(bones);
        ArgumentNullException.ThrowIfNull(inverseBindMatrices);

        if (bones.Length != inverseBindMatrices.Length)
        {
            throw new ArgumentException(
                $"Bone count ({bones.Length}) must match inverse bind matrix count ({inverseBindMatrices.Length}).");
        }

        Name = name ?? "Skeleton";
        Bones = bones;
        InverseBindMatrices = inverseBindMatrices;
        RootBoneIndex = rootBoneIndex;
    }

    /// <summary>
    /// Finds a bone by name.
    /// </summary>
    /// <param name="boneName">The bone name to find.</param>
    /// <returns>The bone index, or -1 if not found.</returns>
    public int FindBone(string boneName)
    {
        for (var i = 0; i < Bones.Length; i++)
        {
            if (string.Equals(Bones[i].Name, boneName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets the bone data at the specified index.
    /// </summary>
    /// <param name="index">The bone index.</param>
    /// <returns>The bone data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public BoneData GetBone(int index)
    {
        if (index < 0 || index >= Bones.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index,
                $"Bone index must be between 0 and {Bones.Length - 1}.");
        }

        return Bones[index];
    }

    /// <summary>
    /// Gets the inverse bind matrix for the specified bone.
    /// </summary>
    /// <param name="boneIndex">The bone index.</param>
    /// <returns>The inverse bind matrix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public Matrix4x4 GetInverseBindMatrix(int boneIndex)
    {
        if (boneIndex < 0 || boneIndex >= InverseBindMatrices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(boneIndex), boneIndex,
                $"Bone index must be between 0 and {InverseBindMatrices.Length - 1}.");
        }

        return InverseBindMatrices[boneIndex];
    }

    /// <summary>
    /// Gets all child bone indices for a given bone.
    /// </summary>
    /// <param name="parentIndex">The parent bone index.</param>
    /// <returns>Array of child bone indices.</returns>
    public int[] GetChildBones(int parentIndex)
    {
        var children = new List<int>();

        for (var i = 0; i < Bones.Length; i++)
        {
            if (Bones[i].ParentIndex == parentIndex)
            {
                children.Add(i);
            }
        }

        return [.. children];
    }

    /// <summary>
    /// Checks if this skeleton is compatible with an animation asset.
    /// </summary>
    /// <param name="targetBoneNames">The bone names required by the animation.</param>
    /// <returns>True if all required bones exist in this skeleton.</returns>
    public bool IsCompatibleWith(IReadOnlyList<string> targetBoneNames)
    {
        foreach (var boneName in targetBoneNames)
        {
            if (FindBone(boneName) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the estimated size of this skeleton in bytes.
    /// </summary>
    public long SizeBytes
    {
        get
        {
            // BoneData: ~100 bytes each (name string + int + matrix)
            // Matrix4x4: 64 bytes each
            return (Bones.Length * 100) + (InverseBindMatrices.Length * 64);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // Skeleton assets don't hold GPU resources; GC will clean up the arrays
    }
}
