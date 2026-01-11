namespace KeenEyes.Shaders;

/// <summary>
/// Represents a compiled compute shader ready for execution on the GPU.
/// </summary>
/// <remarks>
/// <para>
/// A CompiledShader is created by <see cref="IGpuDevice.CompileComputeShader"/> and
/// represents GPU-resident shader code. The shader must be disposed when no longer needed
/// to release GPU resources.
/// </para>
/// <para>
/// Thread groups are specified when dispatching the shader. The shader source defines
/// the local workgroup size via numthreads/local_size.
/// </para>
/// </remarks>
public abstract class CompiledShader : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Gets the name of this shader for debugging purposes.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the shader backend this shader was compiled for.
    /// </summary>
    public abstract ShaderBackend Backend { get; }

    /// <summary>
    /// Gets the local workgroup size in the X dimension.
    /// </summary>
    public abstract int LocalSizeX { get; }

    /// <summary>
    /// Gets the local workgroup size in the Y dimension.
    /// </summary>
    public abstract int LocalSizeY { get; }

    /// <summary>
    /// Gets the local workgroup size in the Z dimension.
    /// </summary>
    public abstract int LocalSizeZ { get; }

    /// <summary>
    /// Gets whether this shader has been disposed.
    /// </summary>
    public bool IsDisposed => disposed;

    /// <summary>
    /// Calculates the number of workgroups needed to cover the specified number of items.
    /// </summary>
    /// <param name="itemCount">The total number of items to process.</param>
    /// <returns>The number of workgroups to dispatch in the X dimension.</returns>
    public int CalculateDispatchX(int itemCount)
    {
        return (itemCount + LocalSizeX - 1) / LocalSizeX;
    }

    /// <summary>
    /// Calculates the number of workgroups needed to cover a 2D grid.
    /// </summary>
    /// <param name="width">The width of the grid.</param>
    /// <param name="height">The height of the grid.</param>
    /// <returns>A tuple of (dispatchX, dispatchY).</returns>
    public (int dispatchX, int dispatchY) CalculateDispatch2D(int width, int height)
    {
        return (
            (width + LocalSizeX - 1) / LocalSizeX,
            (height + LocalSizeY - 1) / LocalSizeY
        );
    }

    /// <summary>
    /// Releases GPU resources associated with this shader.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by this shader.
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
    /// Throws <see cref="ObjectDisposedException"/> if this shader has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
