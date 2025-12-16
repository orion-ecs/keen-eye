using System.Numerics;
using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Resources;

/// <summary>
/// Represents a vertex with position, normal, and texture coordinates.
/// </summary>
/// <param name="position">The vertex position in local space.</param>
/// <param name="normal">The vertex normal for lighting calculations.</param>
/// <param name="texCoord">The texture coordinates (UV).</param>
/// <param name="color">The vertex color (RGBA). Defaults to white (Vector4.One) if not specified.</param>
public struct Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, Vector4 color = default)
{
    /// <summary>
    /// The vertex position in local space.
    /// </summary>
    public Vector3 Position = position;

    /// <summary>
    /// The vertex normal for lighting calculations.
    /// </summary>
    public Vector3 Normal = normal;

    /// <summary>
    /// The texture coordinates (UV).
    /// </summary>
    public Vector2 TexCoord = texCoord;

    /// <summary>
    /// The vertex color (RGBA).
    /// </summary>
    public Vector4 Color = color == default ? Vector4.One : color;

    /// <summary>
    /// The size of the vertex structure in bytes.
    /// </summary>
    public static int SizeInBytes => sizeof(float) * 12; // 3+3+2+4
}

/// <summary>
/// Represents mesh data stored on the GPU.
/// </summary>
internal sealed class MeshData : IDisposable
{
    /// <summary>
    /// The Vertex Array Object handle.
    /// </summary>
    public uint Vao { get; init; }

    /// <summary>
    /// The Vertex Buffer Object handle.
    /// </summary>
    public uint Vbo { get; init; }

    /// <summary>
    /// The Element Buffer Object handle.
    /// </summary>
    public uint Ebo { get; init; }

    /// <summary>
    /// The number of indices in the mesh.
    /// </summary>
    public int IndexCount { get; init; }

    /// <summary>
    /// The number of vertices in the mesh.
    /// </summary>
    public int VertexCount { get; init; }

    private bool disposed;

    /// <summary>
    /// Action to delete GPU resources. Set by the MeshManager.
    /// </summary>
    public Action<MeshData>? DeleteAction { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DeleteAction?.Invoke(this);
    }
}

/// <summary>
/// Manages mesh resources on the GPU.
/// </summary>
internal sealed class MeshManager : IDisposable
{
    private readonly Dictionary<int, MeshData> meshes = [];
    private int nextMeshId = 1;
    private bool disposed;

    /// <summary>
    /// Graphics device for GPU operations. Set during initialization.
    /// </summary>
    public IGraphicsDevice? Device { get; set; }

    /// <summary>
    /// Creates a new mesh from vertex and index data.
    /// </summary>
    /// <param name="vertices">The vertex data.</param>
    /// <param name="indices">The index data.</param>
    /// <returns>The mesh resource handle.</returns>
    public int CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("MeshManager not initialized with graphics device");
        }

        uint vao = Device.GenVertexArray();
        uint vbo = Device.GenBuffer();
        uint ebo = Device.GenBuffer();

        Device.BindVertexArray(vao);

        // Upload vertex data
        Device.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        Device.BufferData(BufferTarget.ArrayBuffer, MemoryMarshal.AsBytes(vertices), BufferUsage.StaticDraw);

        // Upload index data
        Device.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        Device.BufferData(BufferTarget.ElementArrayBuffer, MemoryMarshal.AsBytes(indices), BufferUsage.StaticDraw);

        // Set up vertex attributes
        // Position (location 0)
        Device.EnableVertexAttribArray(0);
        Device.VertexAttribPointer(0, 3, VertexAttribType.Float, false, (uint)Vertex.SizeInBytes, 0);

        // Normal (location 1)
        Device.EnableVertexAttribArray(1);
        Device.VertexAttribPointer(1, 3, VertexAttribType.Float, false, (uint)Vertex.SizeInBytes, 3 * sizeof(float));

        // TexCoord (location 2)
        Device.EnableVertexAttribArray(2);
        Device.VertexAttribPointer(2, 2, VertexAttribType.Float, false, (uint)Vertex.SizeInBytes, 6 * sizeof(float));

        // Color (location 3)
        Device.EnableVertexAttribArray(3);
        Device.VertexAttribPointer(3, 4, VertexAttribType.Float, false, (uint)Vertex.SizeInBytes, 8 * sizeof(float));

        Device.BindVertexArray(0);

        var meshData = new MeshData
        {
            Vao = vao,
            Vbo = vbo,
            Ebo = ebo,
            IndexCount = indices.Length,
            VertexCount = vertices.Length,
            DeleteAction = DeleteMeshData
        };

        int id = nextMeshId++;
        meshes[id] = meshData;
        return id;
    }

    /// <summary>
    /// Gets the mesh data for the specified handle.
    /// </summary>
    /// <param name="meshId">The mesh resource handle.</param>
    /// <returns>The mesh data, or null if not found.</returns>
    public MeshData? GetMesh(int meshId)
    {
        return meshes.GetValueOrDefault(meshId);
    }

    /// <summary>
    /// Deletes a mesh resource.
    /// </summary>
    /// <param name="meshId">The mesh resource handle.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteMesh(int meshId)
    {
        if (meshes.Remove(meshId, out var meshData))
        {
            meshData.Dispose();
            return true;
        }
        return false;
    }

    private void DeleteMeshData(MeshData data)
    {
        if (Device is null)
        {
            return;
        }

        Device.DeleteVertexArray(data.Vao);
        Device.DeleteBuffer(data.Vbo);
        Device.DeleteBuffer(data.Ebo);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var mesh in meshes.Values)
        {
            mesh.Dispose();
        }
        meshes.Clear();
    }
}
