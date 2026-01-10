using System.Numerics;

namespace KeenEyes.Assets;

/// <summary>
/// Represents bone joint indices for skeletal animation (up to 4 bone influences per vertex).
/// </summary>
/// <remarks>
/// Joint indices reference bones in a skeleton hierarchy. Each vertex can be influenced
/// by up to 4 bones, with corresponding weights in <see cref="MeshVertex.Weights"/>.
/// </remarks>
/// <param name="Joint0">First bone index.</param>
/// <param name="Joint1">Second bone index.</param>
/// <param name="Joint2">Third bone index.</param>
/// <param name="Joint3">Fourth bone index.</param>
public readonly record struct JointIndices(ushort Joint0, ushort Joint1, ushort Joint2, ushort Joint3)
{
    /// <summary>
    /// Default joint indices (all zero, typically the root bone).
    /// </summary>
    public static JointIndices Default => new(0, 0, 0, 0);
}

/// <summary>
/// Represents a vertex in a mesh with all attributes needed for PBR rendering and skeletal animation.
/// </summary>
/// <remarks>
/// <para>
/// The vertex structure includes all standard glTF vertex attributes:
/// </para>
/// <list type="bullet">
/// <item><description>Position, Normal, TexCoord - Basic geometry</description></item>
/// <item><description>Tangent - For normal mapping (w component is bitangent sign)</description></item>
/// <item><description>Color - Per-vertex color</description></item>
/// <item><description>Joints, Weights - For skeletal animation</description></item>
/// </list>
/// </remarks>
/// <param name="Position">Vertex position in object space.</param>
/// <param name="Normal">Vertex normal vector (normalized).</param>
/// <param name="TexCoord">Texture coordinates (UV).</param>
/// <param name="Tangent">Tangent vector for normal mapping. XYZ is the tangent direction, W is the bitangent sign (+1 or -1).</param>
/// <param name="Color">Per-vertex color (RGBA). Defaults to white if not present in source mesh.</param>
/// <param name="Joints">Bone indices for skeletal animation (up to 4 influences).</param>
/// <param name="Weights">Bone weights for skeletal animation (must sum to 1.0).</param>
public readonly record struct MeshVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector2 TexCoord,
    Vector4 Tangent,
    Vector4 Color,
    JointIndices Joints,
    Vector4 Weights)
{
    /// <summary>
    /// Creates a basic vertex with only position, normal, and texture coordinates.
    /// Other attributes are set to sensible defaults.
    /// </summary>
    /// <param name="position">Vertex position.</param>
    /// <param name="normal">Vertex normal.</param>
    /// <param name="texCoord">Texture coordinates.</param>
    /// <returns>A new vertex with default tangent, color, and bone data.</returns>
    public static MeshVertex CreateBasic(Vector3 position, Vector3 normal, Vector2 texCoord)
        => new(
            position,
            normal,
            texCoord,
            new Vector4(1, 0, 0, 1),  // Default tangent along X axis, positive bitangent sign
            Vector4.One,               // White color
            JointIndices.Default,      // Root bone
            new Vector4(1, 0, 0, 0));  // Full weight on first joint
}

/// <summary>
/// Represents a submesh within a mesh, defining a range of indices that share the same material.
/// </summary>
/// <remarks>
/// A mesh can contain multiple submeshes, each rendered with a different material.
/// This maps to glTF primitives within a mesh.
/// </remarks>
/// <param name="StartIndex">The starting index in the mesh's index buffer.</param>
/// <param name="IndexCount">The number of indices in this submesh.</param>
/// <param name="MaterialIndex">The index of the material to use for this submesh. -1 means no material (use default).</param>
public readonly record struct Submesh(int StartIndex, int IndexCount, int MaterialIndex);

/// <summary>
/// A loaded mesh asset containing vertex and index data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MeshAsset"/> contains the geometry data loaded from a mesh file (e.g., glTF).
/// Unlike texture and audio assets, mesh assets do not hold GPU resources directly.
/// The graphics system should upload the vertex/index data to GPU buffers when rendering.
/// </para>
/// <para>
/// For static meshes that are rendered frequently, consider caching the GPU buffers
/// separately to avoid re-uploading data each frame.
/// </para>
/// <para>
/// A mesh may contain multiple <see cref="Submeshes"/>, each with its own material.
/// This corresponds to glTF primitives within a mesh.
/// </para>
/// </remarks>
/// <param name="name">The mesh name.</param>
/// <param name="vertices">The vertex array.</param>
/// <param name="indices">The index array.</param>
/// <param name="submeshes">The submesh definitions. If empty, the entire mesh is treated as a single submesh.</param>
/// <param name="boundsMin">AABB minimum.</param>
/// <param name="boundsMax">AABB maximum.</param>
public sealed class MeshAsset(
    string name,
    MeshVertex[] vertices,
    uint[] indices,
    Submesh[] submeshes,
    Vector3 boundsMin,
    Vector3 boundsMax) : IDisposable
{
    // Vertex size in bytes: Position(3) + Normal(3) + TexCoord(2) + Tangent(4) + Color(4) + Joints(4 ushorts = 2 floats) + Weights(4)
    private const int VertexFloatCount = 3 + 3 + 2 + 4 + 4 + 4;
    private const int JointsBytesCount = 4 * sizeof(ushort);

    private bool disposed;

    /// <summary>
    /// Gets the name of the mesh.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the vertex data.
    /// </summary>
    public MeshVertex[] Vertices { get; } = vertices;

    /// <summary>
    /// Gets the index data.
    /// </summary>
    public uint[] Indices { get; } = indices;

    /// <summary>
    /// Gets the submesh definitions.
    /// </summary>
    /// <remarks>
    /// Each submesh defines a range of indices that should be rendered with a specific material.
    /// If empty, treat the entire index buffer as a single submesh with material index -1.
    /// </remarks>
    public Submesh[] Submeshes { get; } = submeshes;

    /// <summary>
    /// Gets the axis-aligned bounding box minimum point.
    /// </summary>
    public Vector3 BoundsMin { get; } = boundsMin;

    /// <summary>
    /// Gets the axis-aligned bounding box maximum point.
    /// </summary>
    public Vector3 BoundsMax { get; } = boundsMax;

    /// <summary>
    /// Gets the size of the mesh data in bytes.
    /// </summary>
    public long SizeBytes =>
        (Vertices.Length * VertexFloatCount * sizeof(float)) +
        (Vertices.Length * JointsBytesCount) +
        (Indices.Length * sizeof(uint)) +
        (Submeshes.Length * 3 * sizeof(int));

    /// <summary>
    /// Creates a mesh asset with automatically computed bounds and a single submesh.
    /// </summary>
    /// <param name="name">The mesh name.</param>
    /// <param name="vertices">The vertex array.</param>
    /// <param name="indices">The index array.</param>
    /// <returns>A new mesh asset with computed bounds and a single submesh.</returns>
    public static MeshAsset Create(string name, MeshVertex[] vertices, uint[] indices)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        // Single submesh covering all indices with default material
        var submeshes = new[] { new Submesh(0, indices.Length, -1) };

        return new MeshAsset(name, vertices, indices, submeshes, min, max);
    }

    /// <summary>
    /// Creates a mesh asset with automatically computed bounds and specified submeshes.
    /// </summary>
    /// <param name="name">The mesh name.</param>
    /// <param name="vertices">The vertex array.</param>
    /// <param name="indices">The index array.</param>
    /// <param name="submeshes">The submesh definitions.</param>
    /// <returns>A new mesh asset with computed bounds.</returns>
    public static MeshAsset Create(string name, MeshVertex[] vertices, uint[] indices, Submesh[] submeshes)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        return new MeshAsset(name, vertices, indices, submeshes, min, max);
    }

    /// <summary>
    /// Releases the mesh data.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // Mesh assets don't hold GPU resources; GC will clean up the arrays
    }
}
