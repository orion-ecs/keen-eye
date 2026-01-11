namespace KeenEyes.Shaders;

/// <summary>
/// Interface implemented by generated GPU compute systems.
/// </summary>
/// <remarks>
/// <para>
/// Each KESL compute shader compiles to a class implementing this interface.
/// The generated class handles:
/// - Shader compilation and caching
/// - Buffer binding for ECS components
/// - Query execution against the ECS world
/// </para>
/// <para>
/// Example generated implementation:
/// <code>
/// public partial class PhysicsGpuSystem : IGpuComputeSystem
/// {
///     public QueryDescriptor Query { get; } = QueryDescriptor.Builder("Physics")
///         .Write("Position")
///         .Read("Velocity")
///         .Build();
///
///     public ShaderBackend Backend => ShaderBackend.GLSL;
///
///     public void Execute(IGpuDevice device, World world, float deltaTime)
///     {
///         // Generated execution logic
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IGpuComputeSystem : IDisposable
{
    /// <summary>
    /// Gets the query descriptor defining which components this system accesses.
    /// </summary>
    QueryDescriptor Query { get; }

    /// <summary>
    /// Gets the shader backend this system uses.
    /// </summary>
    ShaderBackend Backend { get; }

    /// <summary>
    /// Gets whether this system has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets whether this system has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Initializes the system, compiling shaders and allocating GPU resources.
    /// </summary>
    /// <param name="device">The GPU device to use.</param>
    /// <exception cref="ShaderCompilationException">Thrown if shader compilation fails.</exception>
    void Initialize(IGpuDevice device);

    /// <summary>
    /// Gets the generated shader source code.
    /// </summary>
    /// <returns>The shader source code.</returns>
    string GetShaderSource();
}

/// <summary>
/// Extended interface for GPU compute systems that integrate with ECS worlds.
/// </summary>
/// <remarks>
/// This interface adds world-aware execution methods that handle component
/// data transfer between the ECS world and GPU buffers.
/// </remarks>
public interface IWorldGpuComputeSystem : IGpuComputeSystem
{
    /// <summary>
    /// Executes the compute shader on matching entities.
    /// </summary>
    /// <param name="device">The GPU device.</param>
    /// <param name="world">The ECS world containing entities.</param>
    /// <param name="deltaTime">The time delta for physics/animation.</param>
    /// <remarks>
    /// This method:
    /// 1. Queries the world for matching entities
    /// 2. Uploads component data to GPU buffers
    /// 3. Dispatches the compute shader
    /// 4. Downloads modified data back to components
    /// </remarks>
    void Execute(IGpuDevice device, IWorld world, float deltaTime);

    /// <summary>
    /// Executes the compute shader asynchronously.
    /// </summary>
    /// <param name="device">The GPU device.</param>
    /// <param name="world">The ECS world containing entities.</param>
    /// <param name="deltaTime">The time delta for physics/animation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when execution finishes.</returns>
    Task ExecuteAsync(
        IGpuDevice device,
        IWorld world,
        float deltaTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal world interface for GPU compute system integration.
/// </summary>
/// <remarks>
/// This interface is defined here to avoid a circular dependency with KeenEyes.Core.
/// The actual World class implements IWorld from KeenEyes.Abstractions.
/// </remarks>
public interface IWorld
{
    /// <summary>
    /// Gets the number of entities in the world.
    /// </summary>
    int EntityCount { get; }
}
