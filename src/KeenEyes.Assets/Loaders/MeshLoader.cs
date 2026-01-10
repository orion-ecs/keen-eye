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
/// The loader extracts all standard vertex attributes:
/// </para>
/// <list type="bullet">
/// <item><description>POSITION, NORMAL, TEXCOORD_0 - Basic geometry</description></item>
/// <item><description>TANGENT - For normal mapping (computed if missing)</description></item>
/// <item><description>COLOR_0 - Per-vertex color</description></item>
/// <item><description>JOINTS_0, WEIGHTS_0 - Skeletal animation data</description></item>
/// </list>
/// <para>
/// Each primitive in the glTF mesh becomes a <see cref="Submesh"/> with its material index.
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
        var submeshes = new List<Submesh>();
        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);

        foreach (var primitive in logicalMesh.Primitives)
        {
            var baseVertex = (uint)vertices.Count;
            var startIndex = indices.Count;

            // Get vertex accessors
            var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var colors = primitive.GetVertexAccessor("COLOR_0")?.AsVector4Array();
            var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            if (positions == null)
            {
                continue;
            }

            // Extract vertices
            for (var i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                var normal = GetValueOrDefault(normals, i, Vector3.UnitY);
                var texCoord = GetValueOrDefault(texCoords, i, Vector2.Zero);
                var tangent = GetValueOrDefault(tangents, i, new Vector4(1, 0, 0, 1));
                var color = GetValueOrDefault(colors, i, Vector4.One);
                var jointVec = GetValueOrDefault(joints, i, Vector4.Zero);
                var weight = GetValueOrDefault(weights, i, new Vector4(1, 0, 0, 0));

                // Convert joint indices from float to ushort
                var jointIndices = new JointIndices(
                    (ushort)jointVec.X,
                    (ushort)jointVec.Y,
                    (ushort)jointVec.Z,
                    (ushort)jointVec.W);

                vertices.Add(new MeshVertex(position, normal, texCoord, tangent, color, jointIndices, weight));

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

            // Get material index (-1 if no material assigned)
            var materialIndex = primitive.Material?.LogicalIndex ?? -1;
            var indexCount = indices.Count - startIndex;

            submeshes.Add(new Submesh(startIndex, indexCount, materialIndex));
        }

        if (vertices.Count == 0)
        {
            throw new InvalidDataException("Mesh contains no vertices");
        }

        // Compute tangents if they weren't provided in the mesh
        var vertexArray = vertices.ToArray();
        var indexArray = indices.ToArray();

        if (!HasValidTangents(vertexArray))
        {
            ComputeTangents(vertexArray, indexArray);
        }

        return new MeshAsset(
            logicalMesh.Name ?? "Mesh",
            vertexArray,
            indexArray,
            [.. submeshes],
            boundsMin,
            boundsMax);
    }

    private static T GetValueOrDefault<T>(IList<T>? array, int index, T defaultValue)
        => array != null && index < array.Count ? array[index] : defaultValue;

    /// <summary>
    /// Checks if the mesh has valid tangents (not all default values).
    /// </summary>
    private static bool HasValidTangents(MeshVertex[] vertices)
    {
        var defaultTangent = new Vector4(1, 0, 0, 1);

        foreach (var vertex in vertices)
        {
            if (vertex.Tangent != defaultTangent)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes tangent vectors for normal mapping using a simplified MikkTSpace-like algorithm.
    /// </summary>
    /// <remarks>
    /// This implements a basic tangent computation based on UV gradients across triangle edges.
    /// For production use, consider integrating the full MikkTSpace algorithm for better results
    /// with mirrored UVs and edge cases.
    /// </remarks>
    private static void ComputeTangents(MeshVertex[] vertices, uint[] indices)
    {
        // Allocate tangent and bitangent accumulators
        var tangents = new Vector3[vertices.Length];
        var bitangents = new Vector3[vertices.Length];

        // Process each triangle
        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = (int)indices[i];
            var i1 = (int)indices[i + 1];
            var i2 = (int)indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            // Position deltas
            var edge1 = v1.Position - v0.Position;
            var edge2 = v2.Position - v0.Position;

            // UV deltas
            var deltaUv1 = v1.TexCoord - v0.TexCoord;
            var deltaUv2 = v2.TexCoord - v0.TexCoord;

            // Calculate tangent and bitangent
            var r = deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y;

            // Avoid division by zero for degenerate triangles
            if (Math.Abs(r) < 1e-6f)
            {
                continue;
            }

            r = 1.0f / r;

            var tangent = new Vector3(
                (deltaUv2.Y * edge1.X - deltaUv1.Y * edge2.X) * r,
                (deltaUv2.Y * edge1.Y - deltaUv1.Y * edge2.Y) * r,
                (deltaUv2.Y * edge1.Z - deltaUv1.Y * edge2.Z) * r);

            var bitangent = new Vector3(
                (-deltaUv2.X * edge1.X + deltaUv1.X * edge2.X) * r,
                (-deltaUv2.X * edge1.Y + deltaUv1.X * edge2.Y) * r,
                (-deltaUv2.X * edge1.Z + deltaUv1.X * edge2.Z) * r);

            // Accumulate for each vertex of the triangle
            tangents[i0] += tangent;
            tangents[i1] += tangent;
            tangents[i2] += tangent;

            bitangents[i0] += bitangent;
            bitangents[i1] += bitangent;
            bitangents[i2] += bitangent;
        }

        // Orthonormalize and store tangents
        for (var i = 0; i < vertices.Length; i++)
        {
            var n = vertices[i].Normal;
            var t = tangents[i];
            var b = bitangents[i];

            // Gram-Schmidt orthogonalize: t' = normalize(t - n * dot(n, t))
            var tangentOrtho = Vector3.Normalize(t - n * Vector3.Dot(n, t));

            // Handle degenerate case
            if (float.IsNaN(tangentOrtho.X))
            {
                tangentOrtho = new Vector3(1, 0, 0);
            }

            // Calculate handedness (bitangent sign)
            // w = sign of dot(cross(n, t), b)
            var cross = Vector3.Cross(n, t);
            var handedness = Vector3.Dot(cross, b) < 0 ? -1.0f : 1.0f;

            // Update vertex with computed tangent
            vertices[i] = vertices[i] with { Tangent = new Vector4(tangentOrtho, handedness) };
        }
    }
}
