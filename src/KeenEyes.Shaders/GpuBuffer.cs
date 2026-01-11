namespace KeenEyes.Shaders;

/// <summary>
/// Represents a typed buffer of data stored on the GPU.
/// </summary>
/// <typeparam name="T">The element type, which must be an unmanaged struct.</typeparam>
/// <remarks>
/// <para>
/// GpuBuffer provides a type-safe interface for transferring data between CPU and GPU.
/// The buffer can be used as input or output for compute shaders depending on its
/// <see cref="Usage"/> flags.
/// </para>
/// <para>
/// For best performance, prefer batch uploads/downloads and avoid frequent transfers.
/// Use <see cref="BufferUsage.DynamicUpload"/> for buffers that change every frame, or
/// <see cref="BufferUsage.Static"/> for data that rarely changes.
/// </para>
/// </remarks>
public abstract class GpuBuffer<T> : IDisposable where T : unmanaged
{
    private bool disposed;

    /// <summary>
    /// Gets the number of elements in this buffer.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// Gets the size of each element in bytes.
    /// </summary>
    public int ElementSize => System.Runtime.InteropServices.Marshal.SizeOf<T>();

    /// <summary>
    /// Gets the total size of this buffer in bytes.
    /// </summary>
    public int SizeInBytes => Count * ElementSize;

    /// <summary>
    /// Gets the usage flags for this buffer.
    /// </summary>
    public abstract BufferUsage Usage { get; }

    /// <summary>
    /// Gets whether this buffer has been disposed.
    /// </summary>
    public bool IsDisposed => disposed;

    /// <summary>
    /// Uploads data from CPU memory to GPU memory.
    /// </summary>
    /// <param name="data">The data to upload.</param>
    /// <exception cref="ArgumentException">Thrown if data length exceeds buffer capacity.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Upload(ReadOnlySpan<T> data);

    /// <summary>
    /// Uploads data from CPU memory starting at a specific offset.
    /// </summary>
    /// <param name="data">The data to upload.</param>
    /// <param name="offset">The offset in elements where to start writing.</param>
    /// <exception cref="ArgumentException">Thrown if offset + data length exceeds buffer capacity.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Upload(ReadOnlySpan<T> data, int offset);

    /// <summary>
    /// Downloads data from GPU memory to CPU memory.
    /// </summary>
    /// <param name="destination">The span to receive the downloaded data.</param>
    /// <exception cref="ArgumentException">Thrown if destination is too small.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Download(Span<T> destination);

    /// <summary>
    /// Downloads data from GPU memory starting at a specific offset.
    /// </summary>
    /// <param name="destination">The span to receive the downloaded data.</param>
    /// <param name="offset">The offset in elements where to start reading.</param>
    /// <exception cref="ArgumentException">Thrown if offset + destination length exceeds buffer capacity.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Download(Span<T> destination, int offset);

    /// <summary>
    /// Downloads all data from GPU memory to a new array.
    /// </summary>
    /// <returns>An array containing the buffer contents.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public T[] DownloadAll()
    {
        ThrowIfDisposed();
        var result = new T[Count];
        Download(result);
        return result;
    }

    /// <summary>
    /// Clears the buffer to zero.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public abstract void Clear();

    /// <summary>
    /// Releases GPU resources associated with this buffer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this buffer.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        // Derived classes override to release GPU resources
        disposed = true;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if this buffer has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
