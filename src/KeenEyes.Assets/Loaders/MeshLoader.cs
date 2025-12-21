using System.Numerics;
using SharpGLTF.Schema2;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for 3D mesh assets from glTF/GLB files using SharpGLTF.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MeshLoader"/> loads 3D meshes from glTF 2.0 files (both .gltf and .glb binary).
/// glTF is the recommended runtime format for 3D assets as it stores mesh data in GPU-ready
/// binary format.
/// </para>
/// <para>
/// For models with multiple meshes, each mesh is extracted and stored as a separate
/// <see cref="MeshAsset"/>. The first mesh in the file is returned.
/// </para>
/// </remarks>
public sealed class MeshLoader : IAssetLoader<MeshAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".gltf", ".glb"];

    /// <inheritdoc />
    public MeshAsset Load(Stream stream, AssetLoadContext context)
    {
        // Read the entire stream into memory (SharpGLTF needs random access)
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        // Load the glTF model
        var model = ModelRoot.ReadGLB(memoryStream);

        // Get the first mesh
        var logicalMesh = model.LogicalMeshes.FirstOrDefault()
            ?? throw new InvalidDataException($"No meshes found in {context.Path}");

        return ExtractMesh(logicalMesh);
    }

    /// <inheritdoc />
    public async Task<MeshAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // glTF parsing is CPU-bound, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(MeshAsset asset)
        => asset.SizeBytes;

    private static MeshAsset ExtractMesh(Mesh logicalMesh)
    {
        var vertices = new List<MeshVertex>();
        var indices = new List<uint>();
        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);

        foreach (var primitive in logicalMesh.Primitives)
        {
            var baseVertex = (uint)vertices.Count;

            // Get vertex accessors
            var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

            if (positions == null)
            {
                continue;
            }

            // Extract vertices
            for (var i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                var normal = normals != null && i < normals.Count
                    ? normals[i]
                    : Vector3.UnitY;
                var texCoord = texCoords != null && i < texCoords.Count
                    ? texCoords[i]
                    : Vector2.Zero;

                vertices.Add(new MeshVertex(position, normal, texCoord));

                // Update bounds
                boundsMin = Vector3.Min(boundsMin, position);
                boundsMax = Vector3.Max(boundsMax, position);
            }

            // Extract indices
            var indexAccessor = primitive.GetIndexAccessor();
            if (indexAccessor != null)
            {
                foreach (var index in indexAccessor.AsIndicesArray())
                {
                    indices.Add(baseVertex + index);
                }
            }
            else
            {
                // No indices, create sequential indices
                for (uint i = 0; i < positions.Count; i++)
                {
                    indices.Add(baseVertex + i);
                }
            }
        }

        if (vertices.Count == 0)
        {
            throw new InvalidDataException("Mesh contains no vertices");
        }

        return new MeshAsset(
            logicalMesh.Name ?? "Mesh",
            [.. vertices],
            [.. indices],
            boundsMin,
            boundsMax);
    }
}
