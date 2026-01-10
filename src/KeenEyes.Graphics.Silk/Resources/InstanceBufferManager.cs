using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Resources;

/// <summary>
/// Represents instance buffer data stored on the GPU.
/// </summary>
internal sealed class InstanceBufferData : IDisposable
{
    /// <summary>
    /// The Vertex Buffer Object handle for instance data.
    /// </summary>
    public uint Vbo { get; init; }

    /// <summary>
    /// The maximum number of instances this buffer can hold.
    /// </summary>
    public int MaxInstances { get; init; }

    /// <summary>
    /// The current number of instances stored in the buffer.
    /// </summary>
    public int CurrentInstanceCount { get; set; }

    private bool disposed;

    /// <summary>
    /// Action to delete GPU resources. Set by the InstanceBufferManager.
    /// </summary>
    public Action<InstanceBufferData>? DeleteAction { get; set; }

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
/// Manages instance buffer resources on the GPU for instanced rendering.
/// </summary>
/// <remarks>
/// <para>
/// Instance buffers store per-instance data (model matrices, color tints) that is used
/// during instanced draw calls to render many copies of the same mesh efficiently.
/// </para>
/// <para>
/// The instance data layout uses vertex attribute locations 4-8:
/// <list type="bullet">
///   <item><description>Locations 4-7: ModelMatrix (mat4 = 4 vec4 columns)</description></item>
///   <item><description>Location 8: ColorTint (vec4)</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class InstanceBufferManager : IDisposable
{
    private readonly Dictionary<int, InstanceBufferData> buffers = [];
    private int nextBufferId = 1;
    private bool disposed;

    /// <summary>
    /// Graphics device for GPU operations. Set during initialization.
    /// </summary>
    public IGraphicsDevice? Device { get; set; }

    /// <summary>
    /// Creates a new instance buffer with the specified capacity.
    /// </summary>
    /// <param name="maxInstances">The maximum number of instances the buffer can hold.</param>
    /// <returns>The instance buffer resource ID.</returns>
    public int CreateInstanceBuffer(int maxInstances)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("InstanceBufferManager not initialized with graphics device");
        }

        if (maxInstances <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInstances), "Must be greater than zero");
        }

        uint vbo = Device.GenBuffer();

        // Allocate GPU buffer with space for maxInstances
        Device.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        var bufferSize = maxInstances * InstanceData.SizeInBytes;
        Device.BufferData(BufferTarget.ArrayBuffer, new ReadOnlySpan<byte>(new byte[bufferSize]), BufferUsage.DynamicDraw);
        Device.BindBuffer(BufferTarget.ArrayBuffer, 0);

        var bufferData = new InstanceBufferData
        {
            Vbo = vbo,
            MaxInstances = maxInstances,
            CurrentInstanceCount = 0,
            DeleteAction = DeleteBufferData
        };

        int id = nextBufferId++;
        buffers[id] = bufferData;
        return id;
    }

    /// <summary>
    /// Updates the instance data in a buffer.
    /// </summary>
    /// <param name="bufferId">The instance buffer resource ID.</param>
    /// <param name="data">The instance data to upload.</param>
    public void UpdateInstanceBuffer(int bufferId, ReadOnlySpan<InstanceData> data)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("InstanceBufferManager not initialized with graphics device");
        }

        if (!buffers.TryGetValue(bufferId, out var bufferData))
        {
            throw new ArgumentException($"Instance buffer {bufferId} not found", nameof(bufferId));
        }

        if (data.Length > bufferData.MaxInstances)
        {
            throw new ArgumentException(
                $"Data length ({data.Length}) exceeds buffer capacity ({bufferData.MaxInstances})",
                nameof(data));
        }

        Device.BindBuffer(BufferTarget.ArrayBuffer, bufferData.Vbo);
        Device.BufferSubData(BufferTarget.ArrayBuffer, 0, MemoryMarshal.AsBytes(data));
        Device.BindBuffer(BufferTarget.ArrayBuffer, 0);

        bufferData.CurrentInstanceCount = data.Length;
    }

    /// <summary>
    /// Gets the instance buffer data for the specified ID.
    /// </summary>
    /// <param name="bufferId">The instance buffer resource ID.</param>
    /// <returns>The buffer data, or null if not found.</returns>
    public InstanceBufferData? GetBuffer(int bufferId)
    {
        return buffers.GetValueOrDefault(bufferId);
    }

    /// <summary>
    /// Binds an instance buffer to a mesh VAO, setting up the per-instance vertex attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called with the target mesh's VAO bound. It sets up vertex attributes
    /// for the instance data at locations 4-8 with a divisor of 1 (advance per instance).
    /// </para>
    /// </remarks>
    /// <param name="bufferId">The instance buffer resource ID.</param>
    public void BindInstanceBufferToVao(int bufferId)
    {
        if (Device is null)
        {
            throw new InvalidOperationException("InstanceBufferManager not initialized with graphics device");
        }

        if (!buffers.TryGetValue(bufferId, out var bufferData))
        {
            throw new ArgumentException($"Instance buffer {bufferId} not found", nameof(bufferId));
        }

        Device.BindBuffer(BufferTarget.ArrayBuffer, bufferData.Vbo);

        // Set up instance attributes
        // ModelMatrix: mat4 at locations 4-7 (4 vec4 columns)
        // Each column is 16 bytes (4 floats)
        uint stride = (uint)InstanceData.SizeInBytes;

        // Column 0 of ModelMatrix (location 4)
        Device.EnableVertexAttribArray(4);
        Device.VertexAttribPointer(4, 4, VertexAttribType.Float, false, stride, 0);
        Device.VertexAttribDivisor(4, 1); // Per-instance

        // Column 1 of ModelMatrix (location 5)
        Device.EnableVertexAttribArray(5);
        Device.VertexAttribPointer(5, 4, VertexAttribType.Float, false, stride, 16);
        Device.VertexAttribDivisor(5, 1);

        // Column 2 of ModelMatrix (location 6)
        Device.EnableVertexAttribArray(6);
        Device.VertexAttribPointer(6, 4, VertexAttribType.Float, false, stride, 32);
        Device.VertexAttribDivisor(6, 1);

        // Column 3 of ModelMatrix (location 7)
        Device.EnableVertexAttribArray(7);
        Device.VertexAttribPointer(7, 4, VertexAttribType.Float, false, stride, 48);
        Device.VertexAttribDivisor(7, 1);

        // ColorTint: vec4 at location 8
        Device.EnableVertexAttribArray(8);
        Device.VertexAttribPointer(8, 4, VertexAttribType.Float, false, stride, 64);
        Device.VertexAttribDivisor(8, 1);
    }

    /// <summary>
    /// Deletes an instance buffer resource.
    /// </summary>
    /// <param name="bufferId">The instance buffer resource ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public bool DeleteBuffer(int bufferId)
    {
        if (buffers.Remove(bufferId, out var bufferData))
        {
            bufferData.Dispose();
            return true;
        }
        return false;
    }

    private void DeleteBufferData(InstanceBufferData data)
    {
        Device?.DeleteBuffer(data.Vbo);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var buffer in buffers.Values)
        {
            buffer.Dispose();
        }
        buffers.Clear();
    }
}
