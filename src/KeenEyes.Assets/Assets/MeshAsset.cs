using System.Numerics;

namespace KeenEyes.Assets;

/// <summary>
/// Represents a vertex in a mesh with position, normal, and texture coordinates.
/// </summary>
public readonly record struct MeshVertex(
    Vector3 Position,
    Vector3 Normal,
    Vector2 TexCoord);

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
/// </remarks>
/// <param name="name">The mesh name.</param>
/// <param name="vertices">The vertex array.</param>
/// <param name="indices">The index array.</param>
/// <param name="boundsMin">AABB minimum.</param>
/// <param name="boundsMax">AABB maximum.</param>
public sealed class MeshAsset(
    string name,
    MeshVertex[] vertices,
    uint[] indices,
    Vector3 boundsMin,
    Vector3 boundsMax) : IDisposable
{
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
        (Vertices.Length * (3 + 3 + 2) * sizeof(float)) +
        (Indices.Length * sizeof(uint));

    /// <summary>
    /// Creates a mesh asset with automatically computed bounds.
    /// </summary>
    /// <param name="name">The mesh name.</param>
    /// <param name="vertices">The vertex array.</param>
    /// <param name="indices">The index array.</param>
    /// <returns>A new mesh asset with computed bounds.</returns>
    public static MeshAsset Create(string name, MeshVertex[] vertices, uint[] indices)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        return new MeshAsset(name, vertices, indices, min, max);
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
