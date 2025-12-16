namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Generic interface for managing graphics resources with opaque handles.
/// </summary>
/// <typeparam name="THandle">The handle type (e.g., <see cref="MeshHandle"/>, <see cref="TextureHandle"/>).</typeparam>
/// <remarks>
/// <para>
/// Resource managers are responsible for the lifecycle of GPU resources:
/// <list type="bullet">
///   <item><description>Allocation and initialization</description></item>
///   <item><description>Handle-to-resource mapping</description></item>
///   <item><description>Deallocation and cleanup</description></item>
/// </list>
/// </para>
/// <para>
/// Handles are opaque identifiers that allow the application to reference resources
/// without direct access to backend-specific types. This enables portability
/// across different graphics APIs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a resource and get a handle
/// var handle = resourceManager.Allocate();
///
/// // Check if handle is valid
/// if (resourceManager.IsValid(handle))
/// {
///     // Use the resource
/// }
///
/// // Release when done
/// resourceManager.Release(handle);
/// </code>
/// </example>
public interface IResourceManager<THandle> : IDisposable
    where THandle : struct
{
    /// <summary>
    /// Gets the number of active resources managed by this instance.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the total capacity before reallocation is needed.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Checks whether a handle refers to a valid, allocated resource.
    /// </summary>
    /// <param name="handle">The handle to check.</param>
    /// <returns>True if the handle is valid and the resource exists.</returns>
    /// <remarks>
    /// A handle may be structurally valid (non-negative ID) but still refer to
    /// a resource that has been released. This method checks both conditions.
    /// </remarks>
    bool IsValid(THandle handle);

    /// <summary>
    /// Releases a resource, freeing its GPU memory.
    /// </summary>
    /// <param name="handle">The handle to release.</param>
    /// <returns>True if the resource was released; false if the handle was invalid.</returns>
    /// <remarks>
    /// After release, the handle becomes invalid and should not be used.
    /// Attempting to use a released handle may result in undefined behavior.
    /// </remarks>
    bool Release(THandle handle);

    /// <summary>
    /// Releases all managed resources.
    /// </summary>
    /// <remarks>
    /// This invalidates all handles previously returned by this manager.
    /// </remarks>
    void ReleaseAll();
}

/// <summary>
/// Interface for resource managers that can enumerate their active handles.
/// </summary>
/// <typeparam name="THandle">The handle type.</typeparam>
public interface IEnumerableResourceManager<THandle> : IResourceManager<THandle>
    where THandle : struct
{
    /// <summary>
    /// Gets all active handles managed by this instance.
    /// </summary>
    /// <returns>An enumerable of all valid handles.</returns>
    /// <remarks>
    /// The returned handles are valid at the time of enumeration.
    /// Modifying resources during enumeration may have undefined behavior.
    /// </remarks>
    IEnumerable<THandle> GetActiveHandles();
}
