using System.Numerics;

namespace KeenEyes.Graphics;

/// <summary>
/// Represents a vertex with position, normal, and texture coordinates.
/// </summary>
public struct Vertex
{
    /// <summary>
    /// The vertex position in local space.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The vertex normal for lighting calculations.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// The texture coordinates (UV).
    /// </summary>
    public Vector2 TexCoord;

    /// <summary>
    /// The vertex color (RGBA).
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// Creates a new vertex with the specified attributes.
    /// </summary>
    public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, Vector4 color)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
        Color = color;
    }

    /// <summary>
    /// Creates a vertex with position, normal, and UV (white color).
    /// </summary>
    public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
        : this(position, normal, texCoord, Vector4.One)
    {
    }

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
    /// The OpenGL Vertex Array Object handle.
    /// </summary>
    public uint Vao { get; init; }

    /// <summary>
    /// The OpenGL Vertex Buffer Object handle.
    /// </summary>
    public uint Vbo { get; init; }

    /// <summary>
    /// The OpenGL Element Buffer Object handle.
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
    /// Silk.NET OpenGL context. Set during initialization.
    /// </summary>
    public Silk.NET.OpenGL.GL? GL { get; set; }

    /// <summary>
    /// Creates a new mesh from vertex and index data.
    /// </summary>
    /// <param name="vertices">The vertex data.</param>
    /// <param name="indices">The index data.</param>
    /// <returns>The mesh resource handle.</returns>
    public int CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        if (GL is null)
        {
            throw new InvalidOperationException("MeshManager not initialized with GL context");
        }

        uint vao = GL.GenVertexArray();
        uint vbo = GL.GenBuffer();
        uint ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);

        // Upload vertex data
        GL.BindBuffer(Silk.NET.OpenGL.BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (Vertex* ptr = vertices)
            {
                GL.BufferData(
                    Silk.NET.OpenGL.BufferTargetARB.ArrayBuffer,
                    (nuint)(vertices.Length * Vertex.SizeInBytes),
                    ptr,
                    Silk.NET.OpenGL.BufferUsageARB.StaticDraw);
            }
        }

        // Upload index data
        GL.BindBuffer(Silk.NET.OpenGL.BufferTargetARB.ElementArrayBuffer, ebo);
        unsafe
        {
            fixed (uint* ptr = indices)
            {
                GL.BufferData(
                    Silk.NET.OpenGL.BufferTargetARB.ElementArrayBuffer,
                    (nuint)(indices.Length * sizeof(uint)),
                    ptr,
                    Silk.NET.OpenGL.BufferUsageARB.StaticDraw);
            }
        }

        // Set up vertex attributes
        // Position (location 0)
        GL.EnableVertexAttribArray(0);
        unsafe
        {
            GL.VertexAttribPointer(0, 3, Silk.NET.OpenGL.VertexAttribPointerType.Float, false,
                (uint)Vertex.SizeInBytes, (void*)0);
        }

        // Normal (location 1)
        GL.EnableVertexAttribArray(1);
        unsafe
        {
            GL.VertexAttribPointer(1, 3, Silk.NET.OpenGL.VertexAttribPointerType.Float, false,
                (uint)Vertex.SizeInBytes, (void*)(3 * sizeof(float)));
        }

        // TexCoord (location 2)
        GL.EnableVertexAttribArray(2);
        unsafe
        {
            GL.VertexAttribPointer(2, 2, Silk.NET.OpenGL.VertexAttribPointerType.Float, false,
                (uint)Vertex.SizeInBytes, (void*)(6 * sizeof(float)));
        }

        // Color (location 3)
        GL.EnableVertexAttribArray(3);
        unsafe
        {
            GL.VertexAttribPointer(3, 4, Silk.NET.OpenGL.VertexAttribPointerType.Float, false,
                (uint)Vertex.SizeInBytes, (void*)(8 * sizeof(float)));
        }

        GL.BindVertexArray(0);

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
        if (GL is null)
        {
            return;
        }

        GL.DeleteVertexArray(data.Vao);
        GL.DeleteBuffer(data.Vbo);
        GL.DeleteBuffer(data.Ebo);
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
