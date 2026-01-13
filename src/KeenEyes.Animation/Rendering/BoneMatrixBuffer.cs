using System.Numerics;

namespace KeenEyes.Animation.Rendering;

/// <summary>
/// Manages a buffer of bone matrices for GPU skinning with dirty tracking.
/// </summary>
/// <remarks>
/// <para>
/// BoneMatrixBuffer tracks which bones have been modified and only uploads changed
/// matrices to the GPU, reducing CPU-GPU transfer overhead for large skeletons or
/// scenes with many animated characters.
/// </para>
/// <para>
/// Each bone has a generation counter. When a bone's matrix is updated with a new
/// generation, the buffer marks itself as dirty. On upload, only bones with generations
/// newer than the last upload are transferred.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var buffer = new BoneMatrixBuffer(128);
///
/// // Update bone matrices from animation system
/// for (int i = 0; i &lt; skeleton.BoneCount; i++)
/// {
///     var worldTransform = GetBoneWorldTransform(i);
///     var finalMatrix = worldTransform * skeleton.InverseBindMatrices[i];
///     buffer.SetBoneMatrix(i, finalMatrix, frameGeneration);
/// }
///
/// // Upload only changed bones to GPU
/// buffer.UploadIfDirty(shader);
/// </code>
/// </example>
public sealed class BoneMatrixBuffer : IDisposable
{
    private readonly Matrix4x4[] matrices;
    private readonly ulong[] generations;
    private readonly int maxBones;
    private ulong lastUploadGeneration;
    private bool isDirty;
    private bool disposed;

    /// <summary>
    /// Gets the maximum number of bones this buffer supports.
    /// </summary>
    public int MaxBones => maxBones;

    /// <summary>
    /// Gets the current bone matrices for direct access.
    /// </summary>
    /// <remarks>
    /// For performance-critical code that needs to iterate all matrices.
    /// Modifications through this array do not update dirty tracking.
    /// </remarks>
    public ReadOnlySpan<Matrix4x4> Matrices => matrices.AsSpan();

    /// <summary>
    /// Gets whether the buffer has changes that need to be uploaded.
    /// </summary>
    public bool IsDirty => isDirty;

    /// <summary>
    /// Creates a new bone matrix buffer with the specified capacity.
    /// </summary>
    /// <param name="maxBones">Maximum number of bones supported (default: 128).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxBones is less than 1.</exception>
    public BoneMatrixBuffer(int maxBones = 128)
    {
        if (maxBones < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBones), maxBones,
                "Max bones must be at least 1.");
        }

        this.maxBones = maxBones;
        matrices = new Matrix4x4[maxBones];
        generations = new ulong[maxBones];

        // Initialize all matrices to identity
        for (var i = 0; i < maxBones; i++)
        {
            matrices[i] = Matrix4x4.Identity;
        }
    }

    /// <summary>
    /// Sets the matrix for a specific bone.
    /// </summary>
    /// <param name="boneIndex">The bone index.</param>
    /// <param name="matrix">The bone's final skinning matrix (world * inverseBindMatrix).</param>
    /// <param name="generation">The generation counter for change tracking.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when boneIndex is out of range.</exception>
    public void SetBoneMatrix(int boneIndex, Matrix4x4 matrix, ulong generation)
    {
        if (boneIndex < 0 || boneIndex >= maxBones)
        {
            throw new ArgumentOutOfRangeException(nameof(boneIndex), boneIndex,
                $"Bone index must be between 0 and {maxBones - 1}.");
        }

        matrices[boneIndex] = matrix;
        generations[boneIndex] = generation;

        // Mark as dirty if this is a newer generation than our last upload
        if (generation > lastUploadGeneration)
        {
            isDirty = true;
        }
    }

    /// <summary>
    /// Gets the matrix for a specific bone.
    /// </summary>
    /// <param name="boneIndex">The bone index.</param>
    /// <returns>The bone's current skinning matrix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when boneIndex is out of range.</exception>
    public Matrix4x4 GetBoneMatrix(int boneIndex)
    {
        if (boneIndex < 0 || boneIndex >= maxBones)
        {
            throw new ArgumentOutOfRangeException(nameof(boneIndex), boneIndex,
                $"Bone index must be between 0 and {maxBones - 1}.");
        }

        return matrices[boneIndex];
    }

    /// <summary>
    /// Gets the generation counter for a specific bone.
    /// </summary>
    /// <param name="boneIndex">The bone index.</param>
    /// <returns>The bone's generation counter.</returns>
    public ulong GetBoneGeneration(int boneIndex)
    {
        if (boneIndex < 0 || boneIndex >= maxBones)
        {
            throw new ArgumentOutOfRangeException(nameof(boneIndex), boneIndex,
                $"Bone index must be between 0 and {maxBones - 1}.");
        }

        return generations[boneIndex];
    }

    /// <summary>
    /// Gets the indices of bones that have changed since the last upload.
    /// </summary>
    /// <returns>Enumerable of bone indices that need updating.</returns>
    public IEnumerable<int> GetDirtyBoneIndices()
    {
        for (var i = 0; i < maxBones; i++)
        {
            if (generations[i] > lastUploadGeneration)
            {
                yield return i;
            }
        }
    }

    /// <summary>
    /// Gets the number of bones that have changed since the last upload.
    /// </summary>
    /// <returns>Count of dirty bones.</returns>
    public int GetDirtyBoneCount()
    {
        var count = 0;
        for (var i = 0; i < maxBones; i++)
        {
            if (generations[i] > lastUploadGeneration)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Marks the buffer as having been uploaded, clearing the dirty state.
    /// </summary>
    /// <remarks>
    /// Call this after successfully uploading matrices to the GPU.
    /// </remarks>
    public void MarkAsUploaded()
    {
        // Find the maximum generation that was uploaded
        ulong maxGen = 0;
        for (var i = 0; i < maxBones; i++)
        {
            if (generations[i] > maxGen)
            {
                maxGen = generations[i];
            }
        }

        lastUploadGeneration = maxGen;
        isDirty = false;
    }

    /// <summary>
    /// Forces all bones to be re-uploaded on the next upload call.
    /// </summary>
    /// <remarks>
    /// Use this when the GPU buffer has been lost or needs full reinitialization.
    /// </remarks>
    public void Invalidate()
    {
        lastUploadGeneration = 0;
        isDirty = true;
    }

    /// <summary>
    /// Resets all matrices to identity and clears dirty tracking.
    /// </summary>
    public void Reset()
    {
        for (var i = 0; i < maxBones; i++)
        {
            matrices[i] = Matrix4x4.Identity;
            generations[i] = 0;
        }

        lastUploadGeneration = 0;
        isDirty = false;
    }

    /// <summary>
    /// Copies all matrices to a destination array.
    /// </summary>
    /// <param name="destination">The destination array (must be at least MaxBones length).</param>
    /// <param name="count">Number of matrices to copy.</param>
    /// <exception cref="ArgumentException">Thrown when destination is too small.</exception>
    public void CopyTo(Matrix4x4[] destination, int count)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (count > maxBones)
        {
            count = maxBones;
        }

        if (destination.Length < count)
        {
            throw new ArgumentException($"Destination array too small. Need {count}, got {destination.Length}.");
        }

        Array.Copy(matrices, destination, count);
    }

    /// <summary>
    /// Gets a span of matrices for the specified bone count.
    /// </summary>
    /// <param name="boneCount">Number of bones to include.</param>
    /// <returns>Span of bone matrices.</returns>
    public ReadOnlySpan<Matrix4x4> GetMatrices(int boneCount)
    {
        if (boneCount > maxBones)
        {
            boneCount = maxBones;
        }

        return matrices.AsSpan(0, boneCount);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // No GPU resources to clean up - this is CPU-side only
        // Actual GPU buffer management is handled by the render system
    }
}
