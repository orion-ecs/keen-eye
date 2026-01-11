namespace KeenEyes.Shaders;

/// <summary>
/// Represents a GPU device capable of executing compute shaders.
/// </summary>
/// <remarks>
/// <para>
/// IGpuDevice is the main entry point for GPU compute operations. It provides methods
/// for compiling shaders, creating buffers, and executing commands.
/// </para>
/// <para>
/// Implementations are provided for specific graphics APIs:
/// - OpenGL: KeenEyes.Shaders.OpenGL
/// - Vulkan: KeenEyes.Shaders.Vulkan
/// - DirectX: KeenEyes.Shaders.DirectX
/// </para>
/// </remarks>
public interface IGpuDevice : IDisposable
{
    /// <summary>
    /// Gets the name of this GPU device for debugging purposes.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Gets the preferred shader backend for this device.
    /// </summary>
    ShaderBackend PreferredBackend { get; }

    /// <summary>
    /// Gets the maximum workgroup size in the X dimension.
    /// </summary>
    int MaxWorkgroupSizeX { get; }

    /// <summary>
    /// Gets the maximum workgroup size in the Y dimension.
    /// </summary>
    int MaxWorkgroupSizeY { get; }

    /// <summary>
    /// Gets the maximum workgroup size in the Z dimension.
    /// </summary>
    int MaxWorkgroupSizeZ { get; }

    /// <summary>
    /// Gets the maximum number of workgroups that can be dispatched.
    /// </summary>
    int MaxDispatchWorkgroups { get; }

    /// <summary>
    /// Gets whether this device has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Compiles shader source code into a compute shader.
    /// </summary>
    /// <param name="source">The shader source code.</param>
    /// <param name="backend">The shader language of the source.</param>
    /// <param name="name">Optional name for debugging.</param>
    /// <returns>A compiled shader ready for execution.</returns>
    /// <exception cref="ShaderCompilationException">Thrown if compilation fails.</exception>
    CompiledShader CompileComputeShader(string source, ShaderBackend backend, string? name = null);

    /// <summary>
    /// Creates a typed GPU buffer.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="count">The number of elements.</param>
    /// <param name="usage">Usage hints for memory optimization.</param>
    /// <returns>A new GPU buffer.</returns>
    GpuBuffer<T> CreateBuffer<T>(int count, BufferUsage usage = BufferUsage.ShaderReadWrite) where T : unmanaged;

    /// <summary>
    /// Creates a typed GPU buffer initialized with data.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="data">The initial data to upload.</param>
    /// <param name="usage">Usage hints for memory optimization.</param>
    /// <returns>A new GPU buffer containing the data.</returns>
    GpuBuffer<T> CreateBuffer<T>(ReadOnlySpan<T> data, BufferUsage usage = BufferUsage.ShaderReadWrite) where T : unmanaged;

    /// <summary>
    /// Creates a new command buffer for recording GPU commands.
    /// </summary>
    /// <returns>A new command buffer.</returns>
    GpuCommandBuffer CreateCommandBuffer();

    /// <summary>
    /// Submits a command buffer for execution.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to submit.</param>
    /// <remarks>
    /// Submission is asynchronous. Use <see cref="WaitIdle"/> to wait for completion.
    /// </remarks>
    void Submit(GpuCommandBuffer commandBuffer);

    /// <summary>
    /// Waits for all submitted GPU work to complete.
    /// </summary>
    /// <remarks>
    /// This is a synchronization point that blocks until the GPU has finished
    /// executing all submitted commands. Use sparingly for performance.
    /// </remarks>
    void WaitIdle();

    /// <summary>
    /// Creates a fence for tracking GPU command completion.
    /// </summary>
    /// <returns>A new fence in unsignaled state.</returns>
    IGpuFence CreateFence();
}

/// <summary>
/// Represents a synchronization fence for tracking GPU work completion.
/// </summary>
public interface IGpuFence : IDisposable
{
    /// <summary>
    /// Gets whether this fence has been signaled.
    /// </summary>
    bool IsSignaled { get; }

    /// <summary>
    /// Waits for the fence to be signaled.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <returns>True if signaled, false if timed out.</returns>
    bool Wait(TimeSpan timeout);

    /// <summary>
    /// Resets the fence to unsignaled state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Exception thrown when shader compilation fails.
/// </summary>
public class ShaderCompilationException : Exception
{
    /// <summary>
    /// Gets the shader source that failed to compile.
    /// </summary>
    public string? ShaderSource { get; }

    /// <summary>
    /// Gets the compilation error log from the GPU driver.
    /// </summary>
    public string? ErrorLog { get; }

    /// <summary>
    /// Gets the shader backend that was targeted.
    /// </summary>
    public ShaderBackend Backend { get; }

    /// <summary>
    /// Creates a new shader compilation exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="backend">The shader backend.</param>
    /// <param name="shaderSource">The shader source.</param>
    /// <param name="errorLog">The compilation error log.</param>
    public ShaderCompilationException(
        string message,
        ShaderBackend backend,
        string? shaderSource = null,
        string? errorLog = null)
        : base(BuildMessage(message, errorLog))
    {
        Backend = backend;
        ShaderSource = shaderSource;
        ErrorLog = errorLog;
    }

    /// <summary>
    /// Creates a new shader compilation exception with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="backend">The shader backend.</param>
    /// <param name="innerException">The inner exception.</param>
    public ShaderCompilationException(
        string message,
        ShaderBackend backend,
        Exception innerException)
        : base(message, innerException)
    {
        Backend = backend;
    }

    private static string BuildMessage(string message, string? errorLog)
    {
        if (string.IsNullOrEmpty(errorLog))
        {
            return message;
        }
        return $"{message}\n\nCompilation log:\n{errorLog}";
    }
}
