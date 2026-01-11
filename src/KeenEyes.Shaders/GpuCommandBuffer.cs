namespace KeenEyes.Shaders;

/// <summary>
/// A command buffer for recording and submitting GPU commands.
/// </summary>
/// <remarks>
/// <para>
/// GpuCommandBuffer allows batching multiple GPU operations into a single submission,
/// reducing CPU-GPU synchronization overhead. Commands are recorded in order and
/// executed when the buffer is submitted via <see cref="IGpuDevice"/>.
/// </para>
/// <para>
/// After submission, the command buffer can be reset and reused for new commands.
/// </para>
/// </remarks>
public abstract class GpuCommandBuffer : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets whether this command buffer is currently recording.
    /// </summary>
    public abstract bool IsRecording { get; }

    /// <summary>
    /// Gets whether this command buffer has been disposed.
    /// </summary>
    public bool IsDisposed => disposed;

    /// <summary>
    /// Begins recording commands into this buffer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if already recording.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Begin();

    /// <summary>
    /// Ends recording and prepares the buffer for submission.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not currently recording.</exception>
    public abstract void End();

    /// <summary>
    /// Binds a compute shader for subsequent dispatch commands.
    /// </summary>
    /// <param name="shader">The compiled shader to bind.</param>
    /// <exception cref="InvalidOperationException">Thrown if not currently recording.</exception>
    /// <exception cref="ArgumentNullException">Thrown if shader is null.</exception>
    public abstract void BindComputeShader(CompiledShader shader);

    /// <summary>
    /// Binds a buffer to a shader binding point.
    /// </summary>
    /// <typeparam name="T">The buffer element type.</typeparam>
    /// <param name="binding">The binding index.</param>
    /// <param name="buffer">The buffer to bind.</param>
    /// <exception cref="InvalidOperationException">Thrown if not currently recording.</exception>
    /// <exception cref="ArgumentNullException">Thrown if buffer is null.</exception>
    public abstract void BindBuffer<T>(int binding, GpuBuffer<T> buffer) where T : unmanaged;

    /// <summary>
    /// Sets a uniform value by name.
    /// </summary>
    /// <typeparam name="T">The uniform value type.</typeparam>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="InvalidOperationException">Thrown if not currently recording or no shader bound.</exception>
    public abstract void SetUniform<T>(string name, T value) where T : unmanaged;

    /// <summary>
    /// Dispatches the bound compute shader.
    /// </summary>
    /// <param name="groupCountX">Number of workgroups in X dimension.</param>
    /// <param name="groupCountY">Number of workgroups in Y dimension.</param>
    /// <param name="groupCountZ">Number of workgroups in Z dimension.</param>
    /// <exception cref="InvalidOperationException">Thrown if not recording or no shader bound.</exception>
    public abstract void Dispatch(int groupCountX, int groupCountY = 1, int groupCountZ = 1);

    /// <summary>
    /// Dispatches the bound compute shader with automatic workgroup calculation.
    /// </summary>
    /// <param name="shader">The shader to use for workgroup size calculation.</param>
    /// <param name="itemCount">The total number of items to process.</param>
    /// <exception cref="InvalidOperationException">Thrown if not recording.</exception>
    public void DispatchAuto(CompiledShader shader, int itemCount)
    {
        var groupCount = shader.CalculateDispatchX(itemCount);
        Dispatch(groupCount, 1, 1);
    }

    /// <summary>
    /// Inserts a memory barrier to ensure all previous writes are visible.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not recording.</exception>
    public abstract void MemoryBarrier();

    /// <summary>
    /// Copies data between buffers on the GPU.
    /// </summary>
    /// <typeparam name="T">The buffer element type.</typeparam>
    /// <param name="source">The source buffer.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <exception cref="InvalidOperationException">Thrown if not recording.</exception>
    /// <exception cref="ArgumentException">Thrown if buffers have different sizes.</exception>
    public abstract void CopyBuffer<T>(GpuBuffer<T> source, GpuBuffer<T> destination) where T : unmanaged;

    /// <summary>
    /// Copies a range of data between buffers on the GPU.
    /// </summary>
    /// <typeparam name="T">The buffer element type.</typeparam>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceOffset">Offset in elements from source start.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="destinationOffset">Offset in elements from destination start.</param>
    /// <param name="count">Number of elements to copy.</param>
    public abstract void CopyBufferRange<T>(
        GpuBuffer<T> source,
        int sourceOffset,
        GpuBuffer<T> destination,
        int destinationOffset,
        int count) where T : unmanaged;

    /// <summary>
    /// Resets the command buffer for reuse.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if currently recording.</exception>
    public abstract void Reset();

    /// <summary>
    /// Releases resources associated with this command buffer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this command buffer.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        // Derived classes override to release resources
        disposed = true;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if this command buffer has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
