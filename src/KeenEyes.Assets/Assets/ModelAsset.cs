using System.Numerics;

namespace KeenEyes.Assets;

/// <summary>
/// A complete 3D model containing meshes, materials, and texture data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModelAsset"/> is the aggregate asset for loading complete glTF/GLB files.
/// Unlike <see cref="MeshAsset"/> (single mesh) or <see cref="TextureAsset"/> (GPU resource),
/// ModelAsset bundles all CPU-side data needed to instantiate a 3D model in a scene.
/// </para>
/// <para>
/// The asset includes:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Meshes"/> - All mesh geometry with submeshes</description></item>
/// <item><description><see cref="Materials"/> - PBR material properties</description></item>
/// <item><description><see cref="Textures"/> - Embedded or referenced texture data</description></item>
/// <item><description><see cref="Skeleton"/> - Bone hierarchy for skinned mesh animation (optional)</description></item>
/// </list>
/// <para>
/// Each <see cref="Submesh"/> in a mesh references a material by index into the
/// <see cref="Materials"/> array. Materials reference textures by index into the
/// <see cref="Textures"/> array. This allows efficient sharing of textures across materials.
/// </para>
/// </remarks>
/// <param name="name">The model name (usually the filename without extension).</param>
/// <param name="meshes">All meshes in the model.</param>
/// <param name="materials">All materials in the model.</param>
/// <param name="textures">All embedded/external texture data.</param>
/// <param name="skeleton">Optional skeleton for skinned meshes.</param>
public sealed class ModelAsset(
    string name,
    MeshAsset[] meshes,
    MaterialData[] materials,
    TextureData[] textures,
    SkeletonAsset? skeleton = null) : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the model name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets all meshes in the model.
    /// </summary>
    /// <remarks>
    /// Each mesh may contain multiple <see cref="Submesh"/> entries,
    /// each referencing a different material by index.
    /// </remarks>
    public MeshAsset[] Meshes { get; } = meshes;

    /// <summary>
    /// Gets all materials in the model.
    /// </summary>
    /// <remarks>
    /// Materials are referenced by <see cref="Submesh.MaterialIndex"/>.
    /// A material index of -1 means no material (use default).
    /// </remarks>
    public MaterialData[] Materials { get; } = materials;

    /// <summary>
    /// Gets all texture data in the model.
    /// </summary>
    /// <remarks>
    /// Textures are referenced by material texture indices
    /// (e.g., <see cref="MaterialData.BaseColorTextureIndex"/>).
    /// This contains CPU-side pixel data, not GPU resources.
    /// </remarks>
    public TextureData[] Textures { get; } = textures;

    /// <summary>
    /// Gets the skeleton for skinned mesh animation, if this model has one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The skeleton contains the bone hierarchy and inverse bind matrices needed
    /// for GPU skinning. Models with skinned meshes (glTF meshes with joint/weight
    /// attributes) will have a skeleton extracted from the glTF skin data.
    /// </para>
    /// <para>
    /// Use <see cref="HasSkeleton"/> to check if a skeleton is available before
    /// accessing animation features.
    /// </para>
    /// </remarks>
    public SkeletonAsset? Skeleton { get; } = skeleton;

    /// <summary>
    /// Gets a value indicating whether this model has a skeleton for animation.
    /// </summary>
    public bool HasSkeleton => Skeleton is not null;

    /// <summary>
    /// Gets the total size of all model data in bytes.
    /// </summary>
    /// <remarks>
    /// This includes all mesh vertex/index data and all texture pixel data.
    /// Use this for memory budgeting and cache management.
    /// </remarks>
    public long SizeBytes
    {
        get
        {
            long size = 0;

            foreach (var mesh in Meshes)
            {
                size += mesh.SizeBytes;
            }

            foreach (var texture in Textures)
            {
                size += texture.SizeBytes;
            }

            // Materials are small records, estimate ~100 bytes each
            size += Materials.Length * 100;

            // Include skeleton size if present
            if (Skeleton is not null)
            {
                size += Skeleton.SizeBytes;
            }

            return size;
        }
    }

    /// <summary>
    /// Gets the axis-aligned bounding box minimum point encompassing all meshes.
    /// </summary>
    public Vector3 BoundsMin
    {
        get
        {
            if (Meshes.Length == 0)
            {
                return Vector3.Zero;
            }

            var min = new Vector3(float.MaxValue);
            foreach (var mesh in Meshes)
            {
                min = Vector3.Min(min, mesh.BoundsMin);
            }

            return min;
        }
    }

    /// <summary>
    /// Gets the axis-aligned bounding box maximum point encompassing all meshes.
    /// </summary>
    public Vector3 BoundsMax
    {
        get
        {
            if (Meshes.Length == 0)
            {
                return Vector3.Zero;
            }

            var max = new Vector3(float.MinValue);
            foreach (var mesh in Meshes)
            {
                max = Vector3.Max(max, mesh.BoundsMax);
            }

            return max;
        }
    }

    /// <summary>
    /// Gets the material for a submesh, or <see cref="MaterialData.Default"/> if not found.
    /// </summary>
    /// <param name="materialIndex">The material index from <see cref="Submesh.MaterialIndex"/>.</param>
    /// <returns>The material data, or default if index is out of range or -1.</returns>
    public MaterialData GetMaterial(int materialIndex)
    {
        if (materialIndex < 0 || materialIndex >= Materials.Length)
        {
            return MaterialData.Default;
        }

        return Materials[materialIndex];
    }

    /// <summary>
    /// Gets the texture data for a texture index, or null if not found.
    /// </summary>
    /// <param name="textureIndex">The texture index from material properties.</param>
    /// <returns>The texture data, or null if index is out of range or -1.</returns>
    public TextureData? GetTexture(int textureIndex)
    {
        if (textureIndex < 0 || textureIndex >= Textures.Length)
        {
            return null;
        }

        return Textures[textureIndex];
    }

    /// <summary>
    /// Releases all model resources including meshes and textures.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }

        foreach (var texture in Textures)
        {
            texture.Dispose();
        }

        Skeleton?.Dispose();
    }
}
